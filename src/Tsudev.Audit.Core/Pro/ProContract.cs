namespace Tsudev.Audit.Core.Pro;

/// <summary>
/// Dinh danh chung cua TsuDEV Pro - chep NGUYEN VAN tu .standards/docs/PRODUCT_POLICY.md
/// muc 7.4 va AUTH_AND_ACCOUNT.md muc 14.3. MUST NOT tu dat khac (ADR-0002 muc 2.3).
/// </summary>
public static class ProContract
{
    public const string Issuer = "https://tsudev.com/oidc";
    public const string JwksUrl = "https://tsudev.com/oidc/jwks";
    public const string EntitlementsUrl = "https://tsudev.com/api/me/entitlements";
    public const string Scope = "openid tsudev.entitlements";

    /// <summary>Header <c>typ</c> bat buoc cua JWS quyen loi.</summary>
    public const string EntitlementType = "tsu-ent+jwt";

    /// <summary>Thuat toan DUY NHAT duoc nhan.</summary>
    public const string Algorithm = "ES256";

    /// <summary><c>exp</c> &lt;= <c>iat</c> + 7 ngay - JWS song lau hon la sai hop dong, tu choi.</summary>
    public static readonly TimeSpan MaxLifetime = TimeSpan.FromDays(7);

    public const string InstallationHeader = "X-Tsu-Installation";

    /// <summary>Thu muc du lieu cuc bo (ADR-0002 muc 2.3): <c>%LOCALAPPDATA%\tsudev\tsuswico\</c>.</summary>
    public static string DefaultStateDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tsudev", "tsuswico");

    /// <summary>Trang Pro cho ban ngoai cua hang (muc 7.3 "Kenh").</summary>
    public static string ProUrl(string clientId) => "https://tsudev.com/pro?app=" + Uri.EscapeDataString(clientId);
}
