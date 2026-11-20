using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Tsudev.Audit.Core.Pro;

namespace Tsudev.Audit.Tests;

/// <summary>
/// IdP GIA cuc bo (PRO1 P3): dong vai <c>/oidc/token</c>, <c>/oidc/jwks</c>, <c>/oidc/token/revocation</c>,
/// <c>/api/me/entitlements</c> tren <c>https://idp.test</c> qua <see cref="HttpMessageHandler"/> - KHONG co
/// luot goi mang that nao, tsudev.com khong bao gio bi goi trong test. Ky id_token/JWS bang khoa cua
/// <see cref="ProTestKit"/>; refresh token xoay vong va phat hien dung lai nhu AUTH_AND_ACCOUNT muc 7.
/// </summary>
internal sealed class FakeIdp : HttpMessageHandler
{
    public static readonly ProEndpoints Endpoints = new(
        new Uri("https://idp.test/oidc/auth"), new Uri("https://idp.test/oidc/token"),
        new Uri("https://idp.test/oidc/token/revocation"), new Uri("https://idp.test/oidc/jwks"),
        new Uri("https://idp.test/api/me/entitlements"));

    private readonly ProTestKit _kit;
    private readonly ConcurrentDictionary<string, (string challenge, string redirect, string nonce)> _codes = new();
    private readonly HashSet<string> _liveRefresh = new();
    private readonly HashSet<string> _usedRefresh = new();
    private int _counter;

    public FakeIdp(ProTestKit kit) => _kit = kit;

    public List<HttpRequestMessage> Requests { get; } = new();
    public List<string> InstallationHeaders { get; } = new();
    public List<string> Revoked { get; } = new();

    // Nut van de dung ca loi.
    public bool IssueRefreshToken { get; set; } = true;
    public Func<string, string>? NonceOverride { get; set; }
    public int Devices { get; set; } = 2;
    public bool Offline { get; set; }
    public bool BadEntitlementSignature { get; set; }
    public bool SessionRevoked { get; private set; }

    public string Sub { get; set; } = "u-1";

    /// <summary>Dong vai trang dang nhap tsudev.com: nhan URL /oidc/auth, cap ma cho dung luot do.</summary>
    public string Authorize(Uri authUrl)
    {
        var q = Query(authUrl.Query);
        Assert.Equal("code", q["response_type"]);
        Assert.Equal("S256", q["code_challenge_method"]);
        Assert.Equal("consent", q["prompt"]);
        Assert.Equal(ProContract.Scope, q["scope"]);
        var code = "code-" + Interlocked.Increment(ref _counter).ToString(System.Globalization.CultureInfo.InvariantCulture);
        _codes[code] = (q["code_challenge"], q["redirect_uri"], q["nonce"]);
        return code;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
    {
        lock (Requests) Requests.Add(request);
        if (Offline) throw new HttpRequestException("offline (gia)");
        Assert.Equal("idp.test", request.RequestUri!.Host);   // khong bao gio la tsudev.com

        var path = request.RequestUri.AbsolutePath;
        var form = request.Content is null ? new Dictionary<string, string>()
            : Query(await request.Content.ReadAsStringAsync(cancel));
        return path switch
        {
            "/oidc/token" => Token(form),
            "/oidc/jwks" => Json(HttpStatusCode.OK, _kit.JwksJson()),
            "/oidc/token/revocation" => Revoke(form),
            "/api/me/entitlements" => Entitlements(request),
            _ => Json(HttpStatusCode.NotFound, "{}"),
        };
    }

    private HttpResponseMessage Token(Dictionary<string, string> form)
    {
        Assert.Equal(ProTestKit.ClientId, form["client_id"]);
        Assert.False(form.ContainsKey("client_secret"));   // client cong khai
        if (form["grant_type"] == "authorization_code")
        {
            if (!_codes.TryRemove(form["code"], out var c)) return Error("invalid_grant");   // ma dung mot lan
            if (PkcePair.ChallengeOf(form["code_verifier"]) != c.challenge) return Error("invalid_grant");
            if (form["redirect_uri"] != c.redirect) return Error("invalid_grant");
            return Tokens(NonceOverride?.Invoke(c.nonce) ?? c.nonce);
        }
        if (form["grant_type"] == "refresh_token")
        {
            var rt = form["refresh_token"];
            lock (_liveRefresh)
            {
                if (_usedRefresh.Contains(rt))
                {
                    // Dung lai token da xoay ⇒ thu hoi ca cay phien.
                    SessionRevoked = true;
                    _liveRefresh.Clear();
                    return Error("invalid_grant");
                }
                if (!_liveRefresh.Remove(rt)) return Error("invalid_grant");
                _usedRefresh.Add(rt);
            }
            return Tokens(null);
        }
        return Error("unsupported_grant_type");
    }

    private HttpResponseMessage Tokens(string? nonce)
    {
        var n = Interlocked.Increment(ref _counter).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var access = "at-" + n;
        lock (_liveRefresh) _liveTokens.Add(access);
        string? refresh = null;
        if (IssueRefreshToken)
        {
            refresh = "rt-" + n;
            lock (_liveRefresh) _liveRefresh.Add(refresh);
        }
        var body = new Dictionary<string, object?> { ["access_token"] = access, ["token_type"] = "Bearer", ["expires_in"] = 900 };
        if (refresh is not null) body["refresh_token"] = refresh;
        if (nonce is not null) body["id_token"] = IdToken(nonce);
        return Json(HttpStatusCode.OK, JsonSerializer.Serialize(body));
    }

    private readonly HashSet<string> _liveTokens = new();

    public string IdToken(string nonce, string? aud = null, DateTimeOffset? now = null)
    {
        var t = now ?? DateTimeOffset.UtcNow;
        var claims = new Dictionary<string, object?>
        {
            ["iss"] = ProContract.Issuer, ["sub"] = Sub, ["aud"] = aud ?? ProTestKit.ClientId, ["nonce"] = nonce,
            ["iat"] = t.ToUnixTimeSeconds(), ["exp"] = t.AddMinutes(15).ToUnixTimeSeconds(),
        };
        return _kit.Sign(claims, typ: "JWT");
    }

    private HttpResponseMessage Entitlements(HttpRequestMessage req)
    {
        var bearer = req.Headers.Authorization?.Parameter;
        lock (_liveRefresh)
            if (req.Headers.Authorization?.Scheme != "Bearer" || bearer is null || !_liveTokens.Contains(bearer))
                return Json(HttpStatusCode.Unauthorized, """{"error":"invalid_token"}""");
        if (req.Headers.TryGetValues(ProContract.InstallationHeader, out var v)) InstallationHeaders.AddRange(v);
        var now = DateTimeOffset.UtcNow;
        var claims = ProTestKit.Claims(devices: Devices, iat: now.AddMinutes(-1));
        claims["sub"] = Sub;
        var jws = BadEntitlementSignature
            ? _kit.Sign(claims, key: System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256))
            : _kit.Sign(claims);
        return Json(HttpStatusCode.OK, JsonSerializer.Serialize(new { jws }));
    }

    private HttpResponseMessage Revoke(Dictionary<string, string> form)
    {
        Revoked.Add(form["token"]);
        lock (_liveRefresh) _liveRefresh.Remove(form["token"]);
        return new HttpResponseMessage(HttpStatusCode.OK);
    }

    private static HttpResponseMessage Error(string error)
        => Json(HttpStatusCode.BadRequest, JsonSerializer.Serialize(new { error }));

    private static HttpResponseMessage Json(HttpStatusCode code, string body)
        => new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    public static Dictionary<string, string> Query(string q)
        => q.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .ToDictionary(p => Uri.UnescapeDataString(p[0].Replace('+', ' ')),
                          p => p.Length > 1 ? Uri.UnescapeDataString(p[1].Replace('+', ' ')) : "");
}

/// <summary>"Ma hoa" gia: dao byte + tien to - du de test thay tep KHONG chua token ro.</summary>
internal sealed class FakeProtector : ITokenProtector
{
    private static readonly byte[] Magic = "FAKE"u8.ToArray();
    public byte[] Protect(byte[] plaintext) => Magic.Concat(plaintext.Select(b => (byte)~b)).ToArray();
    public byte[]? Unprotect(byte[] c)
        => c.Length >= 4 && c.AsSpan(0, 4).SequenceEqual(Magic) ? c.Skip(4).Select(b => (byte)~b).ToArray() : null;
}

/// <summary>Bo xu ly HTTP canh hang C: MOI luot goi la loi test.</summary>
internal sealed class NoNetworkHandler : HttpMessageHandler
{
    public int Calls;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
    {
        Interlocked.Increment(ref Calls);
        throw new InvalidOperationException("Goi mang khi chua dang nhap: " + request.RequestUri);
    }
}

/// <summary>Lap rap mot ProAccount tren thu muc tam + IdP gia.</summary>
internal sealed class ProRig : IDisposable
{
    public ProTestKit Kit { get; } = new();
    public TempDir Dir { get; } = new();
    public FakeIdp Idp { get; }
    public HttpClient Http { get; }
    public ProAccountStore Store { get; }
    public InstallationIdStore Installs { get; }
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
    public ProAccount Account { get; }

    public ProRig(HttpMessageHandler? handler = null)
    {
        Idp = new FakeIdp(Kit);
        Http = new HttpClient(handler ?? Idp);
        Store = new ProAccountStore(Dir.Path, new FakeProtector());
        Installs = new InstallationIdStore(Dir.Path);
        Account = new ProAccount(Store, Installs, new ProHttp(Http, FakeIdp.Endpoints, ProTestKit.ClientId),
                                 FakeIdp.Endpoints, ProTestKit.ClientId, () => Now) { LockWait = TimeSpan.FromMilliseconds(200) };
    }

    /// <summary>"Trinh duyet" gia: IdP cap ma roi GET that toi cong loopback (kiem ca TcpListener).</summary>
    public Func<Uri, bool> Browser(Func<string, string, string>? tamper = null, string? path = null)
        => url =>
        {
            var q = FakeIdp.Query(url.Query);
            var code = Idp.Authorize(url);
            var query = tamper?.Invoke(code, q["state"])
                        ?? $"code={code}&state={Uri.EscapeDataString(q["state"])}&iss={Uri.EscapeDataString(ProContract.Issuer)}";
            var target = path is null ? q["redirect_uri"] : q["redirect_uri"].Replace("/callback", path, StringComparison.Ordinal);
            _ = Task.Run(async () =>
            {
                using var c = new HttpClient();
                try { await c.GetAsync(target + "?" + query); } catch (HttpRequestException) { }
            });
            return true;
        };

    public Task<LoginOutcome> LoginAsync(Func<Uri, bool>? browser = null, TimeSpan? timeout = null, bool ipv6 = false)
        => Account.LoginAsync(browser ?? Browser(), _ => { }, timeout ?? TimeSpan.FromSeconds(10), CancellationToken.None, ipv6);

    public void Dispose()
    {
        Http.Dispose();
        Kit.Dispose();
        Dir.Dispose();
    }
}
