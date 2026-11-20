namespace Tsudev.Audit.Core.Pro;

/// <summary>
/// Nguon quyen loi cho cac quyet dinh trong app. Tra ve null khi KHONG co quyen
/// loi hop le - chua dang nhap, ngoai tuyen va JWS dem da qua <c>exp</c>, chu ky
/// sai... Moi truong hop do deu la "mien phi", KHONG phai loi (ADR-0002 muc 2.3).
/// </summary>
public interface IEntitlementSource
{
    Entitlement? Current(DateTimeOffset now);
}

/// <summary>JWS da dem tren may, kiem lai MOI lan doc (het han giua chung ⇒ mien phi).</summary>
public sealed class CachedEntitlementSource(string? cachedJws, EntitlementVerifier verifier) : IEntitlementSource
{
    public Entitlement? Current(DateTimeOffset now) => verifier.Verify(cachedJws, now).Value;
}

/// <summary>Chua dang nhap: khong co quyen loi, khong goi mang.</summary>
public sealed class NoEntitlementSource : IEntitlementSource
{
    public static readonly NoEntitlementSource Instance = new();
    public Entitlement? Current(DateTimeOffset now) => null;
}
