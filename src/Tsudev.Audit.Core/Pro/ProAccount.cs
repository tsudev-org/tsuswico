using System.Net.Sockets;

namespace Tsudev.Audit.Core.Pro;

public enum LoginStatus
{
    Success,
    Timeout,
    Denied,
    StateMismatch,
    WrongIssuer,
    TokenRejected,
    IdTokenInvalid,
    NoRefreshToken,
    Offline,
    NoLoopback,
}

public sealed record LoginOutcome(LoginStatus Status, string? Detail = null, AccountInfo? Account = null,
                                  Entitlement? Entitlement = null)
{
    public bool IsSuccess => Status == LoginStatus.Success;
}

public enum RefreshStatus
{
    /// <summary>Chua dang nhap - KHONG co luot goi mang nao.</summary>
    NotSignedIn,
    Updated,
    /// <summary>Mang loi/het gio: dung JWS dem (neu con han), khong loi.</summary>
    Offline,
    /// <summary>tsudev.com tu choi refresh token (het 30 ngay, bi thu hoi): xoa phien, coi nhu mien phi.</summary>
    SessionExpired,
    /// <summary>Phan hoi quyen loi sai (HTTP loi, JWS khong kiem duoc): giu JWS dem cu.</summary>
    Rejected,
    /// <summary>Tien trinh swico khac dang refresh: khong refresh song song (xem <see cref="ProAccountStore.TryLock"/>).</summary>
    Busy,
}

public sealed record RefreshOutcome(RefreshStatus Status, Entitlement? Entitlement, string? Detail = null);

/// <summary>
/// Dang nhap / quyen loi / dang xuat cua Tsu Swico (ADR-0002 muc 2.3, PRO1 P3).
///
/// BAT BIEN HANG C (QU-STD-AUTH, ADR-0002 muc 2.1): khi CHUA dang nhap, moi duong tru
/// <see cref="LoginAsync"/> tra ve ngay ma KHONG goi <see cref="ProHttp"/> va KHONG ghi tep nao.
/// Test <c>ProOfflineGuardTests</c> canh bang bo xu ly HTTP nem loi o moi luot goi.
/// </summary>
public sealed class ProAccount(
    ProAccountStore store,
    InstallationIdStore installations,
    ProHttp http,
    ProEndpoints endpoints,
    string clientId,
    Func<DateTimeOffset> clock)
{
    /// <summary>JWS dem con moi hon muc nay thi lan quet khong goi mang (tsudev.com dem phia RP 24 gio).</summary>
    public static readonly TimeSpan RefreshAfter = TimeSpan.FromHours(24);

    public static readonly TimeSpan LoginTimeout = TimeSpan.FromMinutes(5);

    /// <summary>Cho khoa refresh toi da bao lau (tien trinh swico khac dang refresh).</summary>
    public TimeSpan LockWait { get; init; } = TimeSpan.FromSeconds(10);

    public bool IsSignedIn => store.HasSession;

    public AccountInfo? Account => store.HasSession ? store.ReadAccount() : null;

    /// <summary>Quyen loi tu JWS dem (kiem lai chu ky + <c>exp</c> MOI lan doc). Khong goi mang.</summary>
    public IEntitlementSource CachedSource()
    {
        if (!store.HasSession || store.ReadJwks() is not { } jwks) return NoEntitlementSource.Instance;
        try
        {
            var keys = JwksKeySet.Parse(jwks);
            return new CachedEntitlementSource(store.ReadEntitlementJws(), new EntitlementVerifier(keys, clientId));
        }
        catch (FormatException) { return NoEntitlementSource.Instance; }
    }

    /// <summary>
    /// Nguon quyen loi cho dau mot lan quet TUONG TAC. Chua dang nhap hoac khong tuong tac ⇒ khong goi
    /// mang. Da dang nhap: JWS dem con moi (&lt; 24 gio) thi dung luon; khong thi thu lam moi, mang loi ⇒ dem.
    /// </summary>
    public async Task<IEntitlementSource> ForRunAsync(bool interactive, CancellationToken cancel)
    {
        if (!interactive || !store.HasSession) return NoEntitlementSource.Instance;
        var cached = CachedSource();
        if (cached.Current(clock()) is { } e && clock() - e.IssuedAt < RefreshAfter) return cached;
        var r = await RefreshAsync(cancel).ConfigureAwait(false);
        return r.Status == RefreshStatus.SessionExpired ? NoEntitlementSource.Instance : CachedSource();
    }

    /// <summary>
    /// Authorization Code + PKCE S256 qua loopback (RFC 8252). <paramref name="openBrowser"/> mo trinh duyet
    /// HE THONG (khong WebView); tra false thi nguoi dung tu mo URL da in.
    /// </summary>
    public async Task<LoginOutcome> LoginAsync(Func<Uri, bool> openBrowser, Action<Uri> showUrl, TimeSpan timeout,
                                               CancellationToken cancel, bool preferIPv6 = false)
    {
        LoopbackReceiver receiver;
        try { receiver = LoopbackReceiver.Start(preferIPv6); }
        catch (SocketException ex) { return new LoginOutcome(LoginStatus.NoLoopback, ex.Message); }

        using (receiver)
        {
            var request = AuthorizationRequest.Create(clientId, receiver.RedirectUri);
            var url = request.BuildUrl(endpoints.Authorization);
            showUrl(url);
            openBrowser(url);

            AuthorizationResponse response;
            try { response = await receiver.WaitAsync(timeout, cancel).ConfigureAwait(false); }
            catch (TimeoutException) { return new LoginOutcome(LoginStatus.Timeout); }

            // state TRUOC moi thu khac: phan hoi khong mang state dung la cua ai khac goi vao cong nay.
            if (!request.StateMatches(response.State)) return new LoginOutcome(LoginStatus.StateMismatch);
            if (response.Error is { } err) return new LoginOutcome(LoginStatus.Denied, err);
            if (response.Issuer is { } iss && iss != ProContract.Issuer) return new LoginOutcome(LoginStatus.WrongIssuer, iss);
            if (string.IsNullOrEmpty(response.Code)) return new LoginOutcome(LoginStatus.Denied, "no_code");

            try
            {
                var tokens = await http.ExchangeCodeAsync(response.Code, request.Pkce.Verifier, receiver.RedirectUri, cancel)
                                       .ConfigureAwait(false);
                if (!tokens.IsOk) return new LoginOutcome(LoginStatus.TokenRejected, tokens.Error);
                var t = tokens.Tokens!;

                var jwks = await http.GetJwksAsync(cancel).ConfigureAwait(false);
                IdTokenCheck idCheck;
                try
                {
                    using var keys = JwksKeySet.Parse(jwks);
                    idCheck = IdTokenValidator.Validate(t.IdToken, keys, clientId, request.Nonce, clock());
                }
                catch (FormatException) { return new LoginOutcome(LoginStatus.IdTokenInvalid, "jwks"); }
                if (!idCheck.IsValid) return new LoginOutcome(LoginStatus.IdTokenInvalid, idCheck.Error);
                if (string.IsNullOrEmpty(t.RefreshToken)) return new LoginOutcome(LoginStatus.NoRefreshToken);

                // Mot tai khoan moi tren cung may: bo quyen loi dem cua tai khoan truoc.
                if (store.ReadAccount() is { } old && old.Subject != idCheck.Subject) store.ClearSession();

                var account = new AccountInfo(idCheck.Subject!, clock());
                var installation = installations.GetOrCreate();   // sinh o LAN DANG NHAP DAU (ADR-0002 2.3)
                store.SaveJwks(jwks);
                store.SaveRefreshToken(t.RefreshToken);
                store.SaveAccount(account);

                Entitlement? ent = null;
                try { ent = await FetchEntitlementAsync(t.AccessToken, installation, jwks, cancel).ConfigureAwait(false); }
                catch (Exception ex) when (IsNetwork(ex, cancel)) { /* dang nhap van thanh cong; lan sau lay lai */ }
                return new LoginOutcome(LoginStatus.Success, null, account, ent);
            }
            catch (Exception ex) when (IsNetwork(ex, cancel))
            {
                return new LoginOutcome(LoginStatus.Offline, ex.Message);
            }
        }
    }

    /// <summary>Lam moi access token bang refresh token (xoay vong) roi lay JWS quyen loi moi.</summary>
    public async Task<RefreshOutcome> RefreshAsync(CancellationToken cancel)
    {
        if (!store.HasSession) return new RefreshOutcome(RefreshStatus.NotSignedIn, null);
        using var guard = store.TryLock(LockWait);
        if (guard is null) return new RefreshOutcome(RefreshStatus.Busy, CachedSource().Current(clock()));

        if (store.ReadRefreshToken() is not { } refresh)
        {
            store.ClearSession();   // khong giai ma duoc (tep chep tu tai khoan khac) = khong co phien
            return new RefreshOutcome(RefreshStatus.SessionExpired, null, "unprotect");
        }

        try
        {
            var tokens = await http.RefreshAsync(refresh, cancel).ConfigureAwait(false);
            if (!tokens.IsOk)
            {
                if (tokens.Error is "invalid_grant" or "invalid_client" or "unauthorized_client")
                {
                    store.ClearSession();
                    return new RefreshOutcome(RefreshStatus.SessionExpired, null, tokens.Error);
                }
                return new RefreshOutcome(RefreshStatus.Rejected, CachedSource().Current(clock()), tokens.Error);
            }
            var t = tokens.Tokens!;
            // Ghi token MOI ngay - token cu da het gia tri tu luc nay (xoay vong).
            if (!string.IsNullOrEmpty(t.RefreshToken)) store.SaveRefreshToken(t.RefreshToken);

            var jwks = await http.GetJwksAsync(cancel).ConfigureAwait(false);
            var ent = await FetchEntitlementAsync(t.AccessToken, installations.GetOrCreate(), jwks, cancel)
                .ConfigureAwait(false);
            if (ent is null) return new RefreshOutcome(RefreshStatus.Rejected, CachedSource().Current(clock()), "entitlement");
            return new RefreshOutcome(RefreshStatus.Updated, ent);
        }
        catch (Exception ex) when (IsNetwork(ex, cancel))
        {
            return new RefreshOutcome(RefreshStatus.Offline, CachedSource().Current(clock()), ex.Message);
        }
    }

    /// <summary>
    /// "Dang xuat va xoa du lieu cuc bo": thu hoi refresh token o tsudev.com (co gang, mang loi thi
    /// thoi), roi xoa MOI tep cua app - ke ca ma cai dat. Chua dang nhap ⇒ chi don tep, khong goi mang.
    /// </summary>
    public async Task<bool?> LogoutAsync(CancellationToken cancel)
    {
        bool? revoked = null;
        if (store.HasSession && store.ReadRefreshToken() is { } refresh)
        {
            try { revoked = await http.RevokeAsync(refresh, cancel).ConfigureAwait(false); }
            catch (Exception ex) when (IsNetwork(ex, cancel)) { revoked = false; }
        }
        store.ClearAll();
        return revoked;
    }

    private async Task<Entitlement?> FetchEntitlementAsync(string accessToken, string installation, string jwks,
                                                          CancellationToken cancel)
    {
        var fetch = await http.GetEntitlementAsync(accessToken, installation, cancel).ConfigureAwait(false);
        if (fetch.Jws is null) return null;
        JwksKeySet keys;
        try { keys = JwksKeySet.Parse(jwks); }
        catch (FormatException) { return null; }
        using (keys)
        {
            var check = new EntitlementVerifier(keys, clientId).Verify(fetch.Jws, clock());
            if (!check.IsValid) return null;
            // Chi ghi de khi JWS moi KIEM DUOC - phan hoi sai khong xoa duoc ban dem tot.
            store.SaveJwks(jwks);
            store.SaveEntitlementJws(fetch.Jws);
            return check.Value;
        }
    }

    /// <summary>Loi mang/het gio (khong phai nguoi dung huy) ⇒ "ngoai tuyen", khong phai loi cua app.</summary>
    private static bool IsNetwork(Exception ex, CancellationToken cancel)
        => ex is HttpRequestException or IOException or SocketException ||
           (ex is TaskCanceledException or OperationCanceledException && !cancel.IsCancellationRequested);
}
