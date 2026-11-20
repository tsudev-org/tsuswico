using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tsudev.Audit.Core.Pro;

/// <summary>
/// Ma hoa refresh token truoc khi ghi dia. Ban Windows: DPAPI pham vi CurrentUser
/// (<c>Tsudev.Audit.Windows.DpapiTokenProtector</c>) - tep chep sang tai khoan/may khac la vo dung.
/// Core khong biet DPAPI (net8.0 thuan); test dung bo bao ve gia.
/// </summary>
public interface ITokenProtector
{
    byte[] Protect(byte[] plaintext);

    /// <summary>Giai ma; tep hong / sai tai khoan ⇒ null (coi nhu chua dang nhap), KHONG nem.</summary>
    byte[]? Unprotect(byte[] ciphertext);
}

/// <summary>Thong tin tai khoan hien thi o <c>swico account</c> - chi <c>sub</c>, khong email/ten.</summary>
public sealed record AccountInfo(string Subject, DateTimeOffset SignedInAt);

/// <summary>
/// Cac tep phien dang nhap trong <c>%LOCALAPPDATA%\tsudev\tsuswico\</c> (ADR-0002 muc 2.3).
///
/// BAT BIEN: chi DOC thi khong tao thu muc/tep nao. Chua dang nhap ⇒ <see cref="HasSession"/> = false
/// va khong co lop nao goi mang (hang C, test <c>ProOfflineGuardTests</c>).
/// </summary>
public sealed class ProAccountStore(string directory, ITokenProtector protector)
{
    public const string RefreshTokenFile = "refresh-token.dpapi";
    public const string EntitlementFile = "entitlement.jws";
    public const string JwksFile = "jwks.json";
    public const string AccountFile = "account.json";
    public const string LockFile = "session.lock";

    public string Directory => directory;

    /// <summary>Co phien = co tep refresh token (chua can giai ma duoc).</summary>
    public bool HasSession => File.Exists(PathOf(RefreshTokenFile));

    public string? ReadRefreshToken()
    {
        var bytes = TryReadBytes(RefreshTokenFile);
        if (bytes is null) return null;
        var plain = protector.Unprotect(bytes);
        return plain is null ? null : Encoding.UTF8.GetString(plain);
    }

    public void SaveRefreshToken(string token) => WriteAtomic(RefreshTokenFile, protector.Protect(Encoding.UTF8.GetBytes(token)));

    public string? ReadEntitlementJws() => TryReadText(EntitlementFile)?.Trim();
    public void SaveEntitlementJws(string jws) => WriteAtomic(EntitlementFile, Encoding.ASCII.GetBytes(jws));

    public string? ReadJwks() => TryReadText(JwksFile);
    public void SaveJwks(string json) => WriteAtomic(JwksFile, Encoding.UTF8.GetBytes(json));

    public AccountInfo? ReadAccount()
    {
        try
        {
            if (TryReadText(AccountFile) is not { } text || JsonNode.Parse(text) is not JsonObject o) return null;
            if (o["sub"]?.GetValue<string>() is not { Length: > 0 } sub) return null;
            var at = o["signed_in_at"]?.GetValue<long>() ?? 0;
            return new AccountInfo(sub, DateTimeOffset.FromUnixTimeSeconds(at));
        }
        catch (JsonException) { return null; }
        catch (InvalidOperationException) { return null; }
        catch (FormatException) { return null; }
    }

    public void SaveAccount(AccountInfo info)
    {
        var o = new JsonObject { ["sub"] = info.Subject, ["signed_in_at"] = info.SignedInAt.ToUnixTimeSeconds() };
        WriteAtomic(AccountFile, Encoding.UTF8.GetBytes(o.ToJsonString()));
    }

    /// <summary>Phien chet (refresh token bi tu choi): bo token + quyen loi, GIU ma cai dat (van la thiet bi cu).</summary>
    public void ClearSession()
    {
        foreach (var f in new[] { RefreshTokenFile, EntitlementFile, AccountFile }) TryDelete(f);
    }

    /// <summary>"Dang xuat va xoa du lieu cuc bo" (AUTH_AND_ACCOUNT 14): moi tep cua app trong thu muc nay.</summary>
    public void ClearAll()
    {
        ClearSession();
        foreach (var f in new[] { JwksFile, InstallationIdStore.FileName, LocalStateStore.FileName, LockFile })
            TryDelete(f);
        try
        {
            if (System.IO.Directory.Exists(directory) && !System.IO.Directory.EnumerateFileSystemEntries(directory).Any())
                System.IO.Directory.Delete(directory);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>
    /// Khoa doc quyen quanh luot refresh. VI SAO: refresh token XOAY VONG moi lan dung va tsudev.com
    /// coi mot token cu dung lai la bi danh cap ⇒ thu hoi CA CAY PHIEN (AUTH_AND_ACCOUNT muc 7). Hai
    /// cua so swico mo cung luc ma cung refresh thi lan thu hai tu dang xuat nguoi dung o moi noi.
    /// Khong lay duoc khoa trong <paramref name="wait"/> ⇒ null (ben goi dung JWS dem).
    /// </summary>
    public IDisposable? TryLock(TimeSpan wait)
    {
        System.IO.Directory.CreateDirectory(directory);
        var deadline = DateTime.UtcNow + wait;
        while (true)
        {
            try
            {
                return new FileStream(PathOf(LockFile), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (DateTime.UtcNow < deadline)
            {
                Thread.Sleep(100);
            }
            catch (IOException) { return null; }
        }
    }

    private string PathOf(string name) => Path.Combine(directory, name);

    private byte[]? TryReadBytes(string name)
    {
        try { return File.Exists(PathOf(name)) ? File.ReadAllBytes(PathOf(name)) : null; }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    private string? TryReadText(string name)
        => TryReadBytes(name) is { } b ? Encoding.UTF8.GetString(b) : null;

    private void WriteAtomic(string name, byte[] data)
    {
        System.IO.Directory.CreateDirectory(directory);
        var tmp = PathOf(name) + ".tmp";
        File.WriteAllBytes(tmp, data);
        File.Move(tmp, PathOf(name), overwrite: true);
    }

    private void TryDelete(string name)
    {
        try { if (File.Exists(PathOf(name))) File.Delete(PathOf(name)); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
