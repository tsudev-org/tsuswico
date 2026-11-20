using System.Security.Cryptography;
using System.Text;

namespace Tsudev.Audit.Core.Pro;

/// <summary>
/// Diem cuoi OIDC cua tsudev.com - chep tu discovery
/// <c>https://tsudev.com/oidc/.well-known/openid-configuration</c> (do 20/11/2026).
///
/// VI SAO GHI CUNG ma khong doc discovery moi lan: bot mot luot goi mang, va mot
/// discovery bi gia mao khong the doi diem cuoi cua token/JWKS. Test thay bang
/// <c>https://idp.test/...</c> - bo test KHONG BAO GIO goi tsudev.com.
/// </summary>
public sealed record ProEndpoints(Uri Authorization, Uri Token, Uri Revocation, Uri Jwks, Uri Entitlements)
{
    public static ProEndpoints Production { get; } = new(
        new Uri("https://tsudev.com/oidc/auth"),
        new Uri("https://tsudev.com/oidc/token"),
        new Uri("https://tsudev.com/oidc/token/revocation"),
        new Uri(ProContract.JwksUrl),
        new Uri(ProContract.EntitlementsUrl));
}

/// <summary>Cap PKCE (RFC 7636): verifier 256 bit ngau nhien, challenge = base64url(SHA-256(verifier)).</summary>
public sealed record PkcePair(string Verifier, string Challenge)
{
    public static PkcePair Create()
    {
        var verifier = Base64Url.Encode(RandomNumberGenerator.GetBytes(32));   // 43 ky tu, trong [43, 128]
        return new PkcePair(verifier, ChallengeOf(verifier));
    }

    public static string ChallengeOf(string verifier)
        => Base64Url.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
}

/// <summary>
/// Mot luot dang nhap: <c>state</c> (chong CSRF tren cong loopback - tien trinh khac tren may goi
/// duoc cong nay), <c>nonce</c> (buoc id_token vao dung luot nay), PKCE S256 (ma bi chan giua
/// duong vo dung neu thieu verifier).
/// </summary>
public sealed record AuthorizationRequest(string ClientId, string RedirectUri, string State, string Nonce, PkcePair Pkce)
{
    public static AuthorizationRequest Create(string clientId, string redirectUri)
        => new(clientId, redirectUri, RandomToken(), RandomToken(), PkcePair.Create());

    public static string RandomToken() => Base64Url.Encode(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// <c>prompt=consent</c> la BAT BUOC: oidc-provider (9.x, <c>actions/authorization/scopes.js</c>)
    /// cat <c>offline_access</c> khoi yeu cau khong co no ⇒ khong co refresh token. Trang dong y cua
    /// tsudev.com lam login + consent trong mot buoc nen nguoi dung khong phai bam them.
    /// </summary>
    public Uri BuildUrl(Uri authorizationEndpoint)
    {
        var q = new (string, string)[]
        {
            ("response_type", "code"),
            ("client_id", ClientId),
            ("redirect_uri", RedirectUri),
            ("scope", ProContract.Scope),
            ("state", State),
            ("nonce", Nonce),
            ("code_challenge", Pkce.Challenge),
            ("code_challenge_method", "S256"),
            ("prompt", "consent"),
        };
        var query = string.Join('&', q.Select(p => p.Item1 + "=" + Uri.EscapeDataString(p.Item2)));
        return new Uri(authorizationEndpoint.GetLeftPart(UriPartial.Path) + "?" + query);
    }

    /// <summary>So <c>state</c> thoi gian hang so - gia tri den tu cong loopback ma ai cung goi duoc.</summary>
    public bool StateMatches(string? received)
        => received is not null &&
           CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(received), Encoding.UTF8.GetBytes(State));
}
