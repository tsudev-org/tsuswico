using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Tsudev.Audit.Core.Pro;

/// <summary>Bo token tu <c>/oidc/token</c>.</summary>
public sealed record TokenSet(string AccessToken, string? RefreshToken, string? IdToken);

/// <summary>Ket qua goi <c>/oidc/token</c>: <see cref="Tokens"/> hoac ma loi OAuth (<c>invalid_grant</c>...).</summary>
public sealed record TokenResult(TokenSet? Tokens, string? Error, HttpStatusCode Status)
{
    public bool IsOk => Tokens is not null;
}

/// <summary>Ket qua <c>/api/me/entitlements</c>: JWS compact, hoac ma HTTP + ma loi.</summary>
public sealed record EntitlementFetch(string? Jws, HttpStatusCode Status, string? Error);

/// <summary>
/// Moi luot goi mang cua tinh nang Pro nam O DAY - mot cho de ra soat (QU-STD-AUTH hang C: chua dang
/// nhap thi khong lop nao duoc goi vao day, xem <see cref="ProAccount"/>). Client cong khai
/// (<c>token_endpoint_auth_method: none</c>): khong co secret nao nhung trong exe.
///
/// Loi mang (<see cref="HttpRequestException"/>, het gio) de NEM ra ngoai - ben goi quyet dinh "coi nhu
/// mien phi". Loi giao thuc (4xx/5xx co than JSON) tra ve trong ket qua.
/// </summary>
public sealed class ProHttp(HttpClient http, ProEndpoints endpoints, string clientId)
{
    public Task<TokenResult> ExchangeCodeAsync(string code, string verifier, string redirectUri, CancellationToken cancel)
        => TokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = verifier,
            ["client_id"] = clientId,
        }, cancel);

    public Task<TokenResult> RefreshAsync(string refreshToken, CancellationToken cancel)
        => TokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId,
        }, cancel);

    /// <summary>RFC 7009. Thanh cong hay khong deu khong doi viec xoa du lieu cuc bo.</summary>
    public async Task<bool> RevokeAsync(string refreshToken, CancellationToken cancel)
    {
        using var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = refreshToken,
            ["token_type_hint"] = "refresh_token",
            ["client_id"] = clientId,
        });
        using var res = await http.PostAsync(endpoints.Revocation, body, cancel).ConfigureAwait(false);
        return res.IsSuccessStatusCode;
    }

    public async Task<string> GetJwksAsync(CancellationToken cancel)
    {
        using var res = await http.GetAsync(endpoints.Jwks, cancel).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(cancel).ConfigureAwait(false);
    }

    /// <summary><c>GET /api/me/entitlements</c> kem Bearer + <c>X-Tsu-Installation</c> (AUTH_AND_ACCOUNT 14.3).</summary>
    public async Task<EntitlementFetch> GetEntitlementAsync(string accessToken, string installationId, CancellationToken cancel)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, endpoints.Entitlements);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Headers.TryAddWithoutValidation(ProContract.InstallationHeader, installationId);
        using var res = await http.SendAsync(req, cancel).ConfigureAwait(false);
        var text = await res.Content.ReadAsStringAsync(cancel).ConfigureAwait(false);
        var json = TryParse(text);
        if (!res.IsSuccessStatusCode) return new EntitlementFetch(null, res.StatusCode, Str(json, "error"));
        return new EntitlementFetch(Str(json, "jws"), res.StatusCode, null);
    }

    private async Task<TokenResult> TokenAsync(Dictionary<string, string> form, CancellationToken cancel)
    {
        using var body = new FormUrlEncodedContent(form);
        using var res = await http.PostAsync(endpoints.Token, body, cancel).ConfigureAwait(false);
        var json = TryParse(await res.Content.ReadAsStringAsync(cancel).ConfigureAwait(false));
        if (!res.IsSuccessStatusCode)
            return new TokenResult(null, Str(json, "error") ?? "http_" + ((int)res.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture), res.StatusCode);
        if (Str(json, "access_token") is not { Length: > 0 } access)
            return new TokenResult(null, "invalid_response", res.StatusCode);
        return new TokenResult(new TokenSet(access, Str(json, "refresh_token"), Str(json, "id_token")), null, res.StatusCode);
    }

    private static JsonElement? TryParse(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            return doc.RootElement.ValueKind == JsonValueKind.Object ? doc.RootElement.Clone() : null;
        }
        catch (JsonException) { return null; }
    }

    private static string? Str(JsonElement? e, string name)
        => e is { } o && o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
