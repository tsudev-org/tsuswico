using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tsudev.Audit.Core.Pro;

/// <summary>Ly do mot JWS quyen loi bi tu choi.</summary>
public enum EntitlementError
{
    None,
    Malformed,
    UnsupportedAlgorithm,
    WrongType,
    UnknownKey,
    BadSignature,
    WrongIssuer,
    WrongAudience,
    Expired,
    LifetimeTooLong,
}

/// <summary>Ket qua kiem: <see cref="Value"/> khac null khi va chi khi hop le.</summary>
public sealed record EntitlementCheck(Entitlement? Value, EntitlementError Error)
{
    public bool IsValid => Value is not null;
    internal static EntitlementCheck Fail(EntitlementError e) => new(null, e);
}

/// <summary>
/// Kiem JWS quyen loi bang ECDsa cua .NET BCL - khong them thu vien JWT nao
/// (ADR-0002 muc 2.3, PRODUCT_POLICY muc 7.4):
/// header <c>alg</c> = ES256 (DUY NHAT), <c>typ</c> = tsu-ent+jwt, khoa chon theo <c>kid</c>;
/// payload <c>iss</c>, <c>aud</c> = client_id cua Tsu Swico, <c>exp</c> chua qua, <c>exp</c> - <c>iat</c> &lt;= 7 ngay.
///
/// VI SAO TU KIEM ma khong dung thu vien: bon dieu kien tren la toan bo hop
/// dong; thu vien JWT tong quat mo them duong (alg=none, HS256 voi khoa cong
/// khai, jku/x5u) ma ta phai nho tat tung cai. O day khong co duong nao ngoai ES256.
/// </summary>
public sealed class EntitlementVerifier(JwksKeySet keys, string clientId)
{
    public EntitlementCheck Verify(string? compact, DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(compact)) return EntitlementCheck.Fail(EntitlementError.Malformed);
        var parts = compact.Split('.');
        if (parts.Length != 3 || parts.Any(p => p.Length == 0)) return EntitlementCheck.Fail(EntitlementError.Malformed);

        var headerBytes = Base64Url.TryDecode(parts[0]);
        var payloadBytes = Base64Url.TryDecode(parts[1]);
        var signature = Base64Url.TryDecode(parts[2]);
        if (headerBytes is null || payloadBytes is null || signature is null)
            return EntitlementCheck.Fail(EntitlementError.Malformed);

        try
        {
            using var header = JsonDocument.Parse(headerBytes);
            var h = header.RootElement;
            if (h.ValueKind != JsonValueKind.Object) return EntitlementCheck.Fail(EntitlementError.Malformed);
            if (Str(h, "alg") != ProContract.Algorithm) return EntitlementCheck.Fail(EntitlementError.UnsupportedAlgorithm);
            if (Str(h, "typ") != ProContract.EntitlementType) return EntitlementCheck.Fail(EntitlementError.WrongType);
            // Phan mo rong bat buoc ma ta khong hieu ⇒ RFC 7515 4.1.11 buoc tu choi.
            if (h.TryGetProperty("crit", out _)) return EntitlementCheck.Fail(EntitlementError.Malformed);

            var key = Str(h, "kid") is { } kid ? keys.Find(kid) : null;
            if (key is null) return EntitlementCheck.Fail(EntitlementError.UnknownKey);

            // ES256 trong JWS: chu ky la R||S, 64 byte (IEEE P1363), KHONG phai DER.
            var signingInput = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);
            if (signature.Length != 64 ||
                !key.VerifyData(signingInput, signature, HashAlgorithmName.SHA256,
                                DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                return EntitlementCheck.Fail(EntitlementError.BadSignature);

            using var payload = JsonDocument.Parse(payloadBytes);
            return ReadClaims(payload.RootElement, now);
        }
        catch (JsonException) { return EntitlementCheck.Fail(EntitlementError.Malformed); }
        catch (InvalidOperationException) { return EntitlementCheck.Fail(EntitlementError.Malformed); }
        catch (FormatException) { return EntitlementCheck.Fail(EntitlementError.Malformed); }
    }

    private EntitlementCheck ReadClaims(JsonElement p, DateTimeOffset now)
    {
        if (p.ValueKind != JsonValueKind.Object) return EntitlementCheck.Fail(EntitlementError.Malformed);
        if (Str(p, "iss") != ProContract.Issuer) return EntitlementCheck.Fail(EntitlementError.WrongIssuer);
        if (!AudienceMatches(p)) return EntitlementCheck.Fail(EntitlementError.WrongAudience);

        if (!TryLong(p, "exp", out var exp) || !TryLong(p, "iat", out var iat) || Str(p, "sub") is not { } sub)
            return EntitlementCheck.Fail(EntitlementError.Malformed);
        if (now.ToUnixTimeSeconds() >= exp) return EntitlementCheck.Fail(EntitlementError.Expired);
        if (exp - iat > (long)ProContract.MaxLifetime.TotalSeconds) return EntitlementCheck.Fail(EntitlementError.LifetimeTooLong);

        long proUntil = TryLong(p, "pro_until", out var pu) ? pu : 0;
        int devices = TryLong(p, "devices_30d", out var d) && d is >= 0 and <= int.MaxValue ? (int)d : 0;
        bool donor = p.TryGetProperty("donor", out var dn) && dn.ValueKind == JsonValueKind.True;

        return new EntitlementCheck(new Entitlement(
            sub,
            DateTimeOffset.FromUnixTimeSeconds(iat),
            DateTimeOffset.FromUnixTimeSeconds(exp),
            proUntil,
            Str(p, "pro_source"),
            donor,
            Str(p, "badge") ?? "none",
            devices), EntitlementError.None);
    }

    private bool AudienceMatches(JsonElement p)
    {
        if (!p.TryGetProperty("aud", out var aud)) return false;
        return aud.ValueKind switch
        {
            JsonValueKind.String => aud.GetString() == clientId,
            JsonValueKind.Array => aud.EnumerateArray().Any(a => a.ValueKind == JsonValueKind.String && a.GetString() == clientId),
            _ => false,
        };
    }

    private static string? Str(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static bool TryLong(JsonElement e, string name, out long value)
    {
        value = 0;
        return e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out value);
    }
}
