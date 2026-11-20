using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tsudev.Audit.Core.Pro;

/// <summary>Ket qua kiem id_token: <see cref="Subject"/> khac null khi va chi khi hop le.</summary>
public sealed record IdTokenCheck(string? Subject, string? Error)
{
    public bool IsValid => Subject is not null;
    internal static IdTokenCheck Fail(string error) => new(null, error);
}

/// <summary>
/// Kiem id_token (OIDC Core 3.1.3.7) bang ECDsa cua BCL, cung tinh than <see cref="EntitlementVerifier"/>:
/// CHI <c>alg</c> = ES256 (tsudev.com chi ky ES256 - discovery <c>id_token_signing_alg_values_supported</c>),
/// khoa theo <c>kid</c> tu JWKS, <c>iss</c>, <c>aud</c> = client_id (mang nhieu phan tu ⇒ <c>azp</c> phai
/// = client_id), <c>exp</c>, <c>iat</c> khong o tuong lai, <c>nonce</c> dung luot nay.
/// </summary>
public static class IdTokenValidator
{
    /// <summary>Lech dong ho chap nhan duoc giua may khach va tsudev.com.</summary>
    public static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(5);

    public static IdTokenCheck Validate(string? idToken, JwksKeySet keys, string clientId, string nonce,
                                        DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(idToken)) return IdTokenCheck.Fail("missing");
        var parts = idToken.Split('.');
        if (parts.Length != 3 || parts.Any(p => p.Length == 0)) return IdTokenCheck.Fail("malformed");
        var headerBytes = Base64Url.TryDecode(parts[0]);
        var payloadBytes = Base64Url.TryDecode(parts[1]);
        var signature = Base64Url.TryDecode(parts[2]);
        if (headerBytes is null || payloadBytes is null || signature is null) return IdTokenCheck.Fail("malformed");

        try
        {
            using var header = JsonDocument.Parse(headerBytes);
            var h = header.RootElement;
            if (h.ValueKind != JsonValueKind.Object) return IdTokenCheck.Fail("malformed");
            if (Str(h, "alg") != ProContract.Algorithm) return IdTokenCheck.Fail("alg");
            if (h.TryGetProperty("crit", out _)) return IdTokenCheck.Fail("crit");
            var key = Str(h, "kid") is { } kid ? keys.Find(kid) : null;
            if (key is null) return IdTokenCheck.Fail("kid");
            if (signature.Length != 64 ||
                !key.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), signature,
                                HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                return IdTokenCheck.Fail("signature");

            using var payload = JsonDocument.Parse(payloadBytes);
            var p = payload.RootElement;
            if (p.ValueKind != JsonValueKind.Object) return IdTokenCheck.Fail("malformed");
            if (Str(p, "iss") != ProContract.Issuer) return IdTokenCheck.Fail("iss");
            if (!AudienceOk(p, clientId)) return IdTokenCheck.Fail("aud");
            if (!TryLong(p, "exp", out var exp) || !TryLong(p, "iat", out var iat)) return IdTokenCheck.Fail("malformed");
            var t = now.ToUnixTimeSeconds();
            var skew = (long)ClockSkew.TotalSeconds;
            if (t - skew >= exp) return IdTokenCheck.Fail("exp");
            if (iat > t + skew) return IdTokenCheck.Fail("iat");
            if (Str(p, "nonce") is not { } n ||
                !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(n), Encoding.UTF8.GetBytes(nonce)))
                return IdTokenCheck.Fail("nonce");
            if (Str(p, "sub") is not { Length: > 0 } sub) return IdTokenCheck.Fail("sub");
            return new IdTokenCheck(sub, null);
        }
        catch (JsonException) { return IdTokenCheck.Fail("malformed"); }
        catch (InvalidOperationException) { return IdTokenCheck.Fail("malformed"); }
        catch (FormatException) { return IdTokenCheck.Fail("malformed"); }
    }

    private static bool AudienceOk(JsonElement p, string clientId)
    {
        if (!p.TryGetProperty("aud", out var aud)) return false;
        if (aud.ValueKind == JsonValueKind.String) return aud.GetString() == clientId;
        if (aud.ValueKind != JsonValueKind.Array) return false;
        var list = aud.EnumerateArray().Select(a => a.ValueKind == JsonValueKind.String ? a.GetString() : null).ToList();
        if (!list.Contains(clientId)) return false;
        return list.Count == 1 || Str(p, "azp") == clientId;
    }

    private static string? Str(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static bool TryLong(JsonElement e, string name, out long value)
    {
        value = 0;
        return e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out value);
    }
}
