using System.Net.Sockets;
using System.Text;
using Tsudev.Audit.Core.Cli;
using Tsudev.Audit.Core.Pro;

namespace Tsudev.Audit.Tests;

/// <summary>
/// PRO1 P3 - <c>swico login|logout|account</c> (ADR-0002 muc 2.3) chay tren IdP GIA (<see cref="FakeIdp"/>):
/// loopback that tren 127.0.0.1, PKCE S256, state, nonce, id_token ES256, refresh xoay vong, DPAPI (gia),
/// JWS quyen loi kem X-Tsu-Installation.
/// </summary>
public class ProLoginTests
{
    // ---- Luong day du ----

    [Fact]
    public async Task Dang_nhap_thanh_cong_luu_token_ma_hoa_ma_cai_dat_va_JWS()
    {
        using var rig = new ProRig();
        var o = await rig.LoginAsync();

        Assert.Equal(LoginStatus.Success, o.Status);
        Assert.Equal("u-1", o.Account!.Subject);
        Assert.Equal(2, o.Entitlement!.Devices30d);
        Assert.True(rig.Account.IsSignedIn);

        // Refresh token KHONG nam ro tren dia.
        var raw = File.ReadAllBytes(Path.Combine(rig.Dir.Path, ProAccountStore.RefreshTokenFile));
        Assert.DoesNotContain("rt-", Encoding.UTF8.GetString(raw), StringComparison.Ordinal);
        Assert.StartsWith("rt-", rig.Store.ReadRefreshToken(), StringComparison.Ordinal);

        // Ma cai dat sinh o lan dang nhap dau va di kem luot lay quyen loi.
        var id = rig.Installs.TryGet();
        Assert.True(InstallationId.IsValid(id));
        Assert.Equal(new[] { id! }, rig.Idp.InstallationHeaders);

        Assert.NotNull(rig.Store.ReadEntitlementJws());
        Assert.Equal(2, rig.Account.CachedSource().Current(rig.Now)!.Devices30d);
    }

    [Fact]
    public async Task Redirect_la_127_0_0_1_cong_ngau_nhien_duong_callback()
    {
        using var rig = new ProRig();
        Uri? seen = null;
        var browser = rig.Browser();
        await rig.LoginAsync(u => { seen = u; return browser(u); });
        var redirect = FakeIdp.Query(seen!.Query)["redirect_uri"];
        Assert.Matches(@"^http://127\.0\.0\.1:\d{2,5}/callback$", redirect);
        Assert.StartsWith("https://idp.test/oidc/auth?", seen.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Duong_lui_IPv6_loopback()
    {
        if (!Socket.OSSupportsIPv6) return;
        using var rig = new ProRig();
        Uri? seen = null;
        var browser = rig.Browser();
        LoginOutcome o;
        try { o = await rig.LoginAsync(u => { seen = u; return browser(u); }, ipv6: true); }
        catch (SocketException) { return; }   // runner khong co [::1]
        var redirect = FakeIdp.Query(seen!.Query)["redirect_uri"];
        if (!redirect.StartsWith("http://[::1]:", StringComparison.Ordinal)) return;   // may khong nghe duoc [::1]: da lui ve 127.0.0.1
        Assert.Equal(LoginStatus.Success, o.Status);
    }

    [Fact]
    public async Task Duong_khac_callback_bi_bo_qua_roi_van_nhan_callback()
    {
        using var rig = new ProRig();
        var real = rig.Browser();
        System.Net.HttpStatusCode? other = null;
        var o = await rig.LoginAsync(u =>
        {
            var redirect = FakeIdp.Query(u.Query)["redirect_uri"];
            _ = Task.Run(async () =>
            {
                using var c = new HttpClient();
                var r = await c.GetAsync(redirect.Replace("/callback", "/favicon.ico", StringComparison.Ordinal));
                other = r.StatusCode;
                real(u);
            });
            return true;
        });
        Assert.Equal(LoginStatus.Success, o.Status);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, other);
    }

    // ---- Tu choi ----

    [Fact]
    public async Task State_sai_bi_tu_choi_khong_doi_ma_khong_luu_gi()
    {
        using var rig = new ProRig();
        var o = await rig.LoginAsync(rig.Browser((code, _) => $"code={code}&state=gia-mao"));
        Assert.Equal(LoginStatus.StateMismatch, o.Status);
        Assert.DoesNotContain(rig.Idp.Requests, r => r.RequestUri!.AbsolutePath == "/oidc/token");
        Assert.False(Directory.Exists(rig.Dir.Path));
    }

    [Fact]
    public async Task Thieu_state_bi_tu_choi()
    {
        using var rig = new ProRig();
        var o = await rig.LoginAsync(rig.Browser((code, _) => $"code={code}"));
        Assert.Equal(LoginStatus.StateMismatch, o.Status);
    }

    [Fact]
    public async Task Nguoi_dung_bam_Tu_choi()
    {
        using var rig = new ProRig();
        var o = await rig.LoginAsync(rig.Browser((_, state) => $"error=access_denied&state={Uri.EscapeDataString(state)}"));
        Assert.Equal(LoginStatus.Denied, o.Status);
        Assert.Equal("access_denied", o.Detail);
        Assert.False(rig.Account.IsSignedIn);
    }

    [Fact]
    public async Task Tham_so_iss_khac_bi_tu_choi_RFC_9207()
    {
        using var rig = new ProRig();
        var o = await rig.LoginAsync(rig.Browser((code, state) =>
            $"code={code}&state={Uri.EscapeDataString(state)}&iss={Uri.EscapeDataString("https://evil.test/oidc")}"));
        Assert.Equal(LoginStatus.WrongIssuer, o.Status);
        Assert.False(rig.Account.IsSignedIn);
    }

    [Fact]
    public async Task Nonce_sai_trong_id_token_khong_luu_gi()
    {
        using var rig = new ProRig();
        rig.Idp.NonceOverride = _ => "nonce-cua-luot-khac";
        var o = await rig.LoginAsync();
        Assert.Equal(LoginStatus.IdTokenInvalid, o.Status);
        Assert.Equal("nonce", o.Detail);
        Assert.False(rig.Account.IsSignedIn);
        Assert.Null(rig.Installs.TryGet());
    }

    [Fact]
    public async Task Khong_co_refresh_token_thi_khong_luu_gi()
    {
        using var rig = new ProRig();
        rig.Idp.IssueRefreshToken = false;
        var o = await rig.LoginAsync();
        Assert.Equal(LoginStatus.NoRefreshToken, o.Status);
        Assert.False(rig.Account.IsSignedIn);
    }

    [Fact]
    public async Task Het_gio_khi_khong_ai_dang_nhap()
    {
        using var rig = new ProRig();
        var o = await rig.LoginAsync(_ => false, TimeSpan.FromMilliseconds(300));
        Assert.Equal(LoginStatus.Timeout, o.Status);
        Assert.Empty(rig.Idp.Requests);
    }

    [Fact]
    public async Task Ngoai_tuyen_luc_doi_ma()
    {
        using var rig = new ProRig();
        rig.Idp.Offline = true;
        var o = await rig.LoginAsync();
        Assert.Equal(LoginStatus.Offline, o.Status);
        Assert.False(rig.Account.IsSignedIn);
    }

    [Fact]
    public async Task JWS_quyen_loi_sai_chu_ky_thi_khong_dem_nhung_van_dang_nhap()
    {
        using var rig = new ProRig();
        rig.Idp.BadEntitlementSignature = true;
        var o = await rig.LoginAsync();
        Assert.Equal(LoginStatus.Success, o.Status);
        Assert.Null(o.Entitlement);
        Assert.Null(rig.Store.ReadEntitlementJws());
        Assert.Null(rig.Account.CachedSource().Current(rig.Now));   // = mien phi
    }

    // ---- Lam moi, dem, xoay vong ----

    [Fact]
    public async Task Refresh_xoay_vong_luu_token_moi()
    {
        using var rig = new ProRig();
        await rig.LoginAsync();
        var first = rig.Store.ReadRefreshToken();
        var r = await rig.Account.RefreshAsync(CancellationToken.None);
        Assert.Equal(RefreshStatus.Updated, r.Status);
        Assert.NotEqual(first, rig.Store.ReadRefreshToken());
        Assert.False(rig.Idp.SessionRevoked);

        // Lan nua van dung token MOI - khong bao gio dung lai token da xoay.
        Assert.Equal(RefreshStatus.Updated, (await rig.Account.RefreshAsync(CancellationToken.None)).Status);
        Assert.False(rig.Idp.SessionRevoked);
    }

    [Fact]
    public async Task Refresh_token_bi_tu_choi_xoa_phien_giu_ma_cai_dat()
    {
        using var rig = new ProRig();
        await rig.LoginAsync();
        var id = rig.Installs.TryGet();
        rig.Store.SaveRefreshToken("rt-da-bi-thu-hoi");
        var r = await rig.Account.RefreshAsync(CancellationToken.None);
        Assert.Equal(RefreshStatus.SessionExpired, r.Status);
        Assert.False(rig.Account.IsSignedIn);
        Assert.Null(rig.Store.ReadEntitlementJws());
        Assert.Equal(id, rig.Installs.TryGet());
        Assert.Null(rig.Account.CachedSource().Current(rig.Now));
    }

    [Fact]
    public async Task Ngoai_tuyen_dung_JWS_dem_con_han()
    {
        using var rig = new ProRig();
        await rig.LoginAsync();
        rig.Idp.Offline = true;
        var r = await rig.Account.RefreshAsync(CancellationToken.None);
        Assert.Equal(RefreshStatus.Offline, r.Status);
        Assert.Equal(2, r.Entitlement!.Devices30d);
        Assert.True(rig.Account.IsSignedIn);   // mang loi KHONG lam mat dang nhap
    }

    [Fact]
    public async Task Dem_qua_7_ngay_ngoai_tuyen_thi_coi_la_mien_phi()
    {
        using var rig = new ProRig();
        await rig.LoginAsync();
        rig.Idp.Offline = true;
        rig.Now = rig.Now.AddDays(7).AddMinutes(1);
        var r = await rig.Account.RefreshAsync(CancellationToken.None);
        Assert.Equal(RefreshStatus.Offline, r.Status);
        Assert.Null(r.Entitlement);
        Assert.Null((await rig.Account.ForRunAsync(true, CancellationToken.None)).Current(rig.Now));
    }

    [Fact]
    public async Task Lan_quet_dung_dem_moi_hon_24_gio_khong_goi_mang()
    {
        using var rig = new ProRig();
        await rig.LoginAsync();
        var before = rig.Idp.Requests.Count;
        rig.Now = rig.Now.AddHours(23);
        var src = await rig.Account.ForRunAsync(true, CancellationToken.None);
        Assert.NotNull(src.Current(rig.Now));
        Assert.Equal(before, rig.Idp.Requests.Count);

        rig.Now = rig.Now.AddHours(2);   // > 24 gio ⇒ lam moi
        await rig.Account.ForRunAsync(true, CancellationToken.None);
        Assert.True(rig.Idp.Requests.Count > before);
    }

    [Fact]
    public async Task Lan_quet_khong_tuong_tac_khong_goi_mang_du_da_dang_nhap()
    {
        using var rig = new ProRig();
        await rig.LoginAsync();
        rig.Now = rig.Now.AddDays(2);
        var before = rig.Idp.Requests.Count;
        var src = await rig.Account.ForRunAsync(interactive: false, CancellationToken.None);
        Assert.Same(NoEntitlementSource.Instance, src);
        Assert.Equal(before, rig.Idp.Requests.Count);
    }

    [Fact]
    public async Task Hai_tien_trinh_khong_refresh_song_song()
    {
        using var rig = new ProRig();
        await rig.LoginAsync();
        var before = rig.Idp.Requests.Count;
        using (rig.Store.TryLock(TimeSpan.Zero))
        {
            var r = await rig.Account.RefreshAsync(CancellationToken.None);
            Assert.Equal(RefreshStatus.Busy, r.Status);
            Assert.NotNull(r.Entitlement);   // van co ban dem
        }
        Assert.Equal(before, rig.Idp.Requests.Count);
    }

    [Fact]
    public async Task Token_tep_tu_tai_khoan_khac_khong_giai_duoc_coi_nhu_chua_dang_nhap()
    {
        using var rig = new ProRig();
        await rig.LoginAsync();
        File.WriteAllBytes(Path.Combine(rig.Dir.Path, ProAccountStore.RefreshTokenFile), new byte[] { 1, 2, 3 });
        var r = await rig.Account.RefreshAsync(CancellationToken.None);
        Assert.Equal(RefreshStatus.SessionExpired, r.Status);
        Assert.False(rig.Account.IsSignedIn);
    }

    // ---- Dang xuat ----

    [Fact]
    public async Task Dang_xuat_thu_hoi_token_va_xoa_moi_tep()
    {
        using var rig = new ProRig();
        await rig.LoginAsync();
        new LocalStateStore(rig.Dir.Path).SetTimestamp(ProNotice.LastShownKey, rig.Now);
        var rt = rig.Store.ReadRefreshToken();
        var revoked = await rig.Account.LogoutAsync(CancellationToken.None);
        Assert.True(revoked);
        Assert.Equal(new[] { rt! }, rig.Idp.Revoked);
        Assert.False(Directory.Exists(rig.Dir.Path));   // ca ma cai dat va state.json
    }

    [Fact]
    public async Task Dang_xuat_khi_mat_mang_van_xoa_du_lieu_cuc_bo()
    {
        using var rig = new ProRig();
        await rig.LoginAsync();
        rig.Idp.Offline = true;
        Assert.False(await rig.Account.LogoutAsync(CancellationToken.None));
        Assert.False(Directory.Exists(rig.Dir.Path));
    }

    [Fact]
    public async Task Dang_nhap_tai_khoan_khac_bo_quyen_loi_cua_tai_khoan_truoc()
    {
        using var rig = new ProRig();
        await rig.LoginAsync();
        var id = rig.Installs.TryGet();
        rig.Idp.Sub = "u-2";
        var o = await rig.LoginAsync();
        Assert.Equal("u-2", o.Account!.Subject);
        Assert.Equal("u-2", rig.Store.ReadAccount()!.Subject);
        Assert.Equal(id, rig.Installs.TryGet());   // van la thiet bi cu
    }
}

/// <summary>Kiem id_token (OIDC Core 3.1.3.7) - tung dieu kien mot.</summary>
public class IdTokenValidatorTests
{
    private const string Nonce = "n-123";

    private static IdTokenCheck Check(ProTestKit kit, string token, DateTimeOffset? now = null)
    {
        using var keys = JwksKeySet.Parse(kit.JwksJson());
        return IdTokenValidator.Validate(token, keys, ProTestKit.ClientId, Nonce, now ?? ProTestKit.Now);
    }

    private static Dictionary<string, object?> Claims() => new()
    {
        ["iss"] = ProContract.Issuer, ["sub"] = "u-1", ["aud"] = ProTestKit.ClientId, ["nonce"] = Nonce,
        ["iat"] = ProTestKit.Now.ToUnixTimeSeconds(), ["exp"] = ProTestKit.Now.AddMinutes(15).ToUnixTimeSeconds(),
    };

    [Fact]
    public void Hop_le()
    {
        using var kit = new ProTestKit();
        var c = Check(kit, kit.Sign(Claims(), typ: "JWT"));
        Assert.True(c.IsValid, c.Error);
        Assert.Equal("u-1", c.Subject);
    }

    [Theory]
    [InlineData("iss", "https://evil.test/oidc", "iss")]
    [InlineData("aud", "client-khac", "aud")]
    [InlineData("nonce", "n-khac", "nonce")]
    [InlineData("sub", "", "sub")]
    public void Claim_sai(string claim, string value, string error)
    {
        using var kit = new ProTestKit();
        var c = Claims();
        c[claim] = value;
        Assert.Equal(error, Check(kit, kit.Sign(c, typ: "JWT")).Error);
    }

    [Fact]
    public void Aud_mang_nhieu_phan_tu_can_azp()
    {
        using var kit = new ProTestKit();
        var c = Claims();
        c["aud"] = new[] { ProTestKit.ClientId, "khac" };
        Assert.Equal("aud", Check(kit, kit.Sign(c, typ: "JWT")).Error);
        c["azp"] = ProTestKit.ClientId;
        Assert.True(Check(kit, kit.Sign(c, typ: "JWT")).IsValid);
    }

    [Fact]
    public void Het_han_va_iat_tuong_lai()
    {
        using var kit = new ProTestKit();
        var tok = kit.Sign(Claims(), typ: "JWT");
        Assert.Equal("exp", Check(kit, tok, ProTestKit.Now.AddMinutes(21)).Error);   // 15 phut + 5 lech
        Assert.True(Check(kit, tok, ProTestKit.Now.AddMinutes(19)).IsValid);
        Assert.Equal("iat", Check(kit, tok, ProTestKit.Now.AddMinutes(-6)).Error);
    }

    [Fact]
    public void Chi_nhan_ES256_khoa_dung_kid_chu_ky_dung()
    {
        using var kit = new ProTestKit();
        Assert.Equal("alg", Check(kit, kit.Sign(Claims(), alg: "HS256", typ: "JWT")).Error);
        Assert.Equal("alg", Check(kit, kit.Sign(Claims(), alg: "none", typ: "JWT")).Error);
        Assert.Equal("kid", Check(kit, kit.Sign(Claims(), typ: "JWT", kid: "k-la")).Error);
        using var other = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
        Assert.Equal("signature", Check(kit, kit.Sign(Claims(), typ: "JWT", key: other)).Error);
        Assert.Equal("missing", Check(kit, "").Error);
        Assert.Equal("malformed", Check(kit, "a.b").Error);
    }
}

/// <summary>PKCE, URL uy quyen, phan tich callback.</summary>
public class AuthorizationRequestTests
{
    [Fact]
    public void PKCE_S256_dung_vector_RFC_7636_phu_luc_B()
        => Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
                        PkcePair.ChallengeOf("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"));

    [Fact]
    public void Verifier_state_nonce_ngau_nhien_du_dai()
    {
        var a = AuthorizationRequest.Create("c", "http://127.0.0.1:1/callback");
        var b = AuthorizationRequest.Create("c", "http://127.0.0.1:1/callback");
        Assert.Equal(43, a.Pkce.Verifier.Length);
        Assert.NotEqual(a.State, b.State);
        Assert.NotEqual(a.Nonce, b.Nonce);
        Assert.NotEqual(a.Pkce.Verifier, b.Pkce.Verifier);
        Assert.True(a.StateMatches(a.State));
        Assert.False(a.StateMatches(b.State));
        Assert.False(a.StateMatches(null));
    }

    [Fact]
    public void URL_co_du_tham_so_va_prompt_consent()
    {
        var r = AuthorizationRequest.Create(ProContract.ClientId, "http://127.0.0.1:50123/callback");
        var q = FakeIdp.Query(r.BuildUrl(ProEndpoints.Production.Authorization).Query);
        Assert.Equal("code", q["response_type"]);
        Assert.Equal("tsuswico", q["client_id"]);
        Assert.Equal("http://127.0.0.1:50123/callback", q["redirect_uri"]);
        Assert.Equal("openid offline_access tsudev.entitlements", q["scope"]);
        Assert.Equal("S256", q["code_challenge_method"]);
        Assert.Equal(PkcePair.ChallengeOf(r.Pkce.Verifier), q["code_challenge"]);
        Assert.Equal("consent", q["prompt"]);   // thieu ⇒ oidc-provider bo offline_access
        Assert.Equal(r.State, q["state"]);
        Assert.Equal(r.Nonce, q["nonce"]);
        Assert.DoesNotContain("code_verifier", q.Keys);
    }

    [Fact]
    public void Diem_cuoi_production_dung_discovery_tsudev()
    {
        var e = ProEndpoints.Production;
        Assert.Equal("https://tsudev.com/oidc/auth", e.Authorization.AbsoluteUri);
        Assert.Equal("https://tsudev.com/oidc/token", e.Token.AbsoluteUri);
        Assert.Equal("https://tsudev.com/oidc/token/revocation", e.Revocation.AbsoluteUri);
        Assert.Equal("https://tsudev.com/oidc/jwks", e.Jwks.AbsoluteUri);
        Assert.Equal("https://tsudev.com/api/me/entitlements", e.Entitlements.AbsoluteUri);
    }

    [Fact]
    public void Callback_lap_tham_so_la_sai_dang()
    {
        Assert.Same(AuthorizationResponse.Malformed, AuthorizationResponse.Parse("code=a&code=b&state=s"));
        var r = AuthorizationResponse.Parse("?code=a%2Bb&state=s+1&iss=https%3A%2F%2Ftsudev.com%2Foidc");
        Assert.Equal("a+b", r.Code);
        Assert.Equal("s 1", r.State);
        Assert.Equal(ProContract.Issuer, r.Issuer);
    }
}

/// <summary>Ba lenh tai khoan tren dong lenh - chi tuong tac.</summary>
public class AccountCommandParseTests
{
    [Theory]
    [InlineData("login", CliCommand.Login)]
    [InlineData("LOGOUT", CliCommand.Logout)]
    [InlineData("account", CliCommand.Account)]
    public void Nhan_ba_lenh(string word, CliCommand expected)
        => Assert.Equal(expected, CliOptions.Parse(new[] { word }).Command);

    [Fact]
    public void Mac_dinh_van_la_quet()
    {
        Assert.Equal(CliCommand.Scan, CliOptions.Parse(Array.Empty<string>()).Command);
        Assert.Equal(CliCommand.Scan, CliOptions.Parse(new[] { "--scope", "license" }).Command);
    }

    [Fact]
    public void Nhan_lang_verbose_log_file()
    {
        var o = CliOptions.Parse(new[] { "login", "--lang", "en", "-v", "--log-file", "a.jsonl" });
        Assert.Equal(CliCommand.Login, o.Command);
        Assert.True(o.Verbose);
        Assert.Equal("a.jsonl", o.LogFile);
    }

    [Theory]
    [InlineData("login", "--silent")]
    [InlineData("account", "--json-only")]
    [InlineData("logout", "-s")]
    [InlineData("login", "--scope")]
    [InlineData("account", "--no-update-check")]
    [InlineData("login", "thua")]
    public void Tu_choi_che_do_khong_tuong_tac_va_tham_so_quet(string cmd, string extra)
        => Assert.Throws<ArgumentException>(() => CliOptions.Parse(new[] { cmd, extra }));

    [Fact]
    public void Lenh_chi_o_vi_tri_dau()
        => Assert.Throws<ArgumentException>(() => CliOptions.Parse(new[] { "--verbose", "login" }));
}
