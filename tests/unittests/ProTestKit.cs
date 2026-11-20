using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tsudev.Audit.Core.Pro;

namespace Tsudev.Audit.Tests;

/// <summary>
/// JWKS GIA + ky JWS ES256 that bang khoa sinh trong test (ADR-0002 muc 3, P2):
/// kiem duoc ca duong chu ky ma khong can tsudev.com.
/// </summary>
internal sealed class ProTestKit : IDisposable
{
    public const string ClientId = "tsuswico-test";
    public const string Kid = "k1";
    public static readonly DateTimeOffset Now = new(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);

    public ECDsa Key { get; } = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    public string JwksJson(params (string kid, ECDsa key)[] extra)
    {
        var keys = new List<object> { Jwk(Kid, Key) };
        keys.AddRange(extra.Select(e => Jwk(e.kid, e.key)));
        return JsonSerializer.Serialize(new { keys });
    }

    public static object Jwk(string kid, ECDsa key)
    {
        var p = key.ExportParameters(false);
        return new { kty = "EC", crv = "P-256", kid, use = "sig", alg = "ES256",
                     x = Base64Url.Encode(p.Q.X), y = Base64Url.Encode(p.Q.Y) };
    }

    public EntitlementVerifier Verifier(string? jwks = null) => new(JwksKeySet.Parse(jwks ?? JwksJson()), ClientId);

    public static Dictionary<string, object?> Claims(int devices = 1, long proUntil = 0,
                                                     DateTimeOffset? iat = null, TimeSpan? life = null)
    {
        var i = iat ?? Now.AddHours(-1);
        return new()
        {
            ["iss"] = ProContract.Issuer, ["sub"] = "u-1", ["aud"] = ClientId,
            ["iat"] = i.ToUnixTimeSeconds(), ["exp"] = i.Add(life ?? TimeSpan.FromDays(7)).ToUnixTimeSeconds(),
            ["pro_until"] = proUntil, ["pro_source"] = null, ["donor"] = false,
            ["badge"] = "none", ["devices_30d"] = devices,
        };
    }

    public string Sign(Dictionary<string, object?> claims, string alg = "ES256", string typ = "tsu-ent+jwt",
                       string kid = Kid, ECDsa? key = null)
    {
        var h = Base64Url.Encode(JsonSerializer.SerializeToUtf8Bytes(new { alg, typ, kid }));
        var p = Base64Url.Encode(JsonSerializer.SerializeToUtf8Bytes(claims));
        var sig = (key ?? Key).SignData(Encoding.ASCII.GetBytes(h + "." + p), HashAlgorithmName.SHA256,
                                        DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return $"{h}.{p}.{Base64Url.Encode(sig)}";
    }

    public Entitlement Ent(int devices = 2, long proUntil = 0)
        => Verifier().Verify(Sign(Claims(devices, proUntil)), Now).Value!;

    public void Dispose() => Key.Dispose();
}

/// <summary>Nguon quyen loi gia cho test ProNotice (PRODUCT_POLICY 7.3 "nguon quyen loi gia").</summary>
internal sealed class FakeEntitlementSource(Entitlement? value) : IEntitlementSource
{
    public Entitlement? Current(DateTimeOffset now) => value;
}

/// <summary>Thu muc tam rieng moi test, tu xoa.</summary>
internal sealed class TempDir : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tsuswico-test-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(Path)) Directory.Delete(Path, true); }
}
