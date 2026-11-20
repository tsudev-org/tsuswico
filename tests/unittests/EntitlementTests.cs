using System.Security.Cryptography;
using System.Text;
using Tsudev.Audit.Core.Pro;

namespace Tsudev.Audit.Tests;

/// <summary>Kiem JWS quyen loi (PRODUCT_POLICY 7.4, ADR-0002 muc 2.3) voi JWKS gia.</summary>
public class EntitlementTests
{
    private static readonly DateTimeOffset Now = ProTestKit.Now;

    [Fact]
    public void JWS_hop_le_doc_du_truong()
    {
        using var kit = new ProTestKit();
        var c = ProTestKit.Claims(devices: 3, proUntil: Now.AddDays(10).ToUnixTimeSeconds());
        c["pro_source"] = "web"; c["donor"] = true; c["badge"] = "both";
        var r = kit.Verifier().Verify(kit.Sign(c), Now);

        Assert.True(r.IsValid);
        Assert.Equal(EntitlementError.None, r.Error);
        var e = r.Value!;
        Assert.Equal("u-1", e.Subject);
        Assert.Equal(3, e.Devices30d);
        Assert.Equal("web", e.ProSource);
        Assert.True(e.Donor);
        Assert.Equal("both", e.Badge);
        Assert.True(e.IsProAt(Now));
        Assert.False(e.IsProAt(Now.AddDays(11)));
    }

    [Fact]
    public void Pro_until_bang_0_la_chua_tung_co_Pro()
    {
        using var kit = new ProTestKit();
        Assert.False(kit.Ent(proUntil: 0).IsProAt(Now));
    }

    [Fact]
    public void Aud_dang_mang_chua_client_id_van_nhan()
    {
        using var kit = new ProTestKit();
        var c = ProTestKit.Claims();
        c["aud"] = new[] { "khac", ProTestKit.ClientId };
        Assert.True(kit.Verifier().Verify(kit.Sign(c), Now).IsValid);
    }

    [Theory]
    [InlineData("HS256")]
    [InlineData("none")]
    [InlineData("ES384")]
    [InlineData("RS256")]
    [InlineData("es256")]
    public void Chi_nhan_dung_ES256(string alg)
    {
        using var kit = new ProTestKit();
        Assert.Equal(EntitlementError.UnsupportedAlgorithm,
                     kit.Verifier().Verify(kit.Sign(ProTestKit.Claims(), alg: alg), Now).Error);
    }

    [Fact]
    public void Alg_none_khong_chu_ky_bi_tu_choi()
    {
        using var kit = new ProTestKit();
        var parts = kit.Sign(ProTestKit.Claims(), alg: "none").Split('.');
        Assert.False(kit.Verifier().Verify($"{parts[0]}.{parts[1]}.", Now).IsValid);
    }

    [Theory]
    [InlineData("JWT")]
    [InlineData("at+jwt")]
    [InlineData("")]
    public void Sai_typ_bi_tu_choi(string typ)
    {
        using var kit = new ProTestKit();
        Assert.Equal(EntitlementError.WrongType,
                     kit.Verifier().Verify(kit.Sign(ProTestKit.Claims(), typ: typ), Now).Error);
    }

    [Fact]
    public void Kid_la_bi_tu_choi()
    {
        using var kit = new ProTestKit();
        Assert.Equal(EntitlementError.UnknownKey,
                     kit.Verifier().Verify(kit.Sign(ProTestKit.Claims(), kid: "k-khac"), Now).Error);
    }

    [Fact]
    public void Chon_khoa_theo_kid_trong_JWKS_nhieu_khoa()
    {
        using var kit = new ProTestKit();
        using var k2 = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var v = kit.Verifier(kit.JwksJson(("k2", k2)));
        Assert.True(v.Verify(kit.Sign(ProTestKit.Claims(), kid: "k2", key: k2), Now).IsValid);
        // Ky bang k2 nhung khai kid=k1 ⇒ sai chu ky.
        Assert.Equal(EntitlementError.BadSignature, v.Verify(kit.Sign(ProTestKit.Claims(), kid: "k1", key: k2), Now).Error);
    }

    [Fact]
    public void Sua_payload_sau_khi_ky_bi_tu_choi()
    {
        using var kit = new ProTestKit();
        var parts = kit.Sign(ProTestKit.Claims(devices: 1)).Split('.');
        var forged = ProTestKit.Claims(devices: 1, proUntil: Now.AddYears(5).ToUnixTimeSeconds());
        var p = Base64Url.Encode(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(forged));
        Assert.Equal(EntitlementError.BadSignature, kit.Verifier().Verify($"{parts[0]}.{p}.{parts[2]}", Now).Error);
    }

    [Fact]
    public void Chu_ky_dang_DER_khong_duoc_nhan()
    {
        using var kit = new ProTestKit();
        var parts = kit.Sign(ProTestKit.Claims()).Split('.');
        var der = kit.Key.SignData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), HashAlgorithmName.SHA256,
                                   DSASignatureFormat.Rfc3279DerSequence);
        Assert.Equal(EntitlementError.BadSignature,
                     kit.Verifier().Verify($"{parts[0]}.{parts[1]}.{Base64Url.Encode(der)}", Now).Error);
    }

    [Fact]
    public void Sai_iss_bi_tu_choi()
    {
        using var kit = new ProTestKit();
        var c = ProTestKit.Claims(); c["iss"] = "https://evil.example/oidc";
        Assert.Equal(EntitlementError.WrongIssuer, kit.Verifier().Verify(kit.Sign(c), Now).Error);
    }

    [Fact]
    public void Sai_aud_bi_tu_choi()
    {
        using var kit = new ProTestKit();
        var c = ProTestKit.Claims(); c["aud"] = "tsufiles";
        Assert.Equal(EntitlementError.WrongAudience, kit.Verifier().Verify(kit.Sign(c), Now).Error);
    }

    [Fact]
    public void Qua_exp_la_het_han_dung_tai_moc()
    {
        using var kit = new ProTestKit();
        var jws = kit.Sign(ProTestKit.Claims(iat: Now.AddDays(-7), life: TimeSpan.FromDays(7)));
        Assert.Equal(EntitlementError.Expired, kit.Verifier().Verify(jws, Now).Error);
        Assert.True(kit.Verifier().Verify(jws, Now.AddSeconds(-1)).IsValid);
    }

    [Fact]
    public void Song_qua_7_ngay_bi_tu_choi()
    {
        using var kit = new ProTestKit();
        var jws = kit.Sign(ProTestKit.Claims(life: TimeSpan.FromDays(7) + TimeSpan.FromSeconds(1)));
        Assert.Equal(EntitlementError.LifetimeTooLong, kit.Verifier().Verify(jws, Now).Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a.b")]
    [InlineData("a.b.c.d")]
    [InlineData("!!.@@.##")]
    [InlineData("e30.e30.AAAA")]          // {} {} - thieu alg
    public void JWS_hong_khong_nem_loi(string? jws)
    {
        using var kit = new ProTestKit();
        var r = kit.Verifier().Verify(jws, Now);
        Assert.False(r.IsValid);
        Assert.NotEqual(EntitlementError.None, r.Error);
    }

    [Fact]
    public void Header_co_crit_bi_tu_choi()
    {
        using var kit = new ProTestKit();
        var h = Base64Url.Encode(Encoding.UTF8.GetBytes("{\"alg\":\"ES256\",\"typ\":\"tsu-ent+jwt\",\"kid\":\"k1\",\"crit\":[\"x\"]}"));
        var p = Base64Url.Encode(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(ProTestKit.Claims()));
        var sig = kit.Key.SignData(Encoding.ASCII.GetBytes(h + "." + p), HashAlgorithmName.SHA256,
                                   DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        Assert.Equal(EntitlementError.Malformed, kit.Verifier().Verify($"{h}.{p}.{Base64Url.Encode(sig)}", Now).Error);
    }

    [Fact]
    public void JWKS_bo_qua_khoa_khong_phai_P256_va_bao_loi_JSON_hong()
    {
        using var kit = new ProTestKit();
        var json = "{\"keys\":[{\"kty\":\"RSA\",\"kid\":\"r\",\"n\":\"AQAB\",\"e\":\"AQAB\"}," +
                   "{\"kty\":\"EC\",\"crv\":\"P-384\",\"kid\":\"p384\",\"x\":\"AA\",\"y\":\"AA\"}," +
                   System.Text.Json.JsonSerializer.Serialize(ProTestKit.Jwk("k1", kit.Key)) + "]}";
        using var set = JwksKeySet.Parse(json);
        Assert.Equal(1, set.Count);
        Assert.NotNull(set.Find("k1"));
        Assert.Throws<FormatException>(() => JwksKeySet.Parse("{khong phai json"));
        Assert.Throws<FormatException>(() => JwksKeySet.Parse("{\"x\":1}"));
    }

    [Fact]
    public void Nguon_dem_het_han_la_mien_phi_khong_loi()
    {
        using var kit = new ProTestKit();
        var src = new CachedEntitlementSource(kit.Sign(ProTestKit.Claims(devices: 4)), kit.Verifier());
        Assert.NotNull(src.Current(Now));
        Assert.Null(src.Current(Now.AddDays(8)));
        Assert.Null(new CachedEntitlementSource(null, kit.Verifier()).Current(Now));
        Assert.Null(NoEntitlementSource.Instance.Current(Now));
    }

    [Fact]
    public void Hop_dong_dung_nguyen_van_PRODUCT_POLICY_7_4()
    {
        Assert.Equal("https://tsudev.com/oidc", ProContract.Issuer);
        Assert.Equal("https://tsudev.com/oidc/jwks", ProContract.JwksUrl);
        Assert.Equal("https://tsudev.com/api/me/entitlements", ProContract.EntitlementsUrl);
        Assert.Equal("tsu-ent+jwt", ProContract.EntitlementType);
        Assert.Equal("X-Tsu-Installation", ProContract.InstallationHeader);
        Assert.Equal("openid tsudev.entitlements", ProContract.Scope);
        Assert.Equal("https://tsudev.com/pro?app=tsuswico", ProContract.ProUrl("tsuswico"));
    }
}
