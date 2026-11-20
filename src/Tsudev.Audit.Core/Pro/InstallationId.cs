using System.Security.Cryptography;

namespace Tsudev.Audit.Core.Pro;

/// <summary>
/// Ma cai dat (AUTH_AND_ACCOUNT muc 14.3): 128 bit tu <see cref="RandomNumberGenerator"/>,
/// base64url khong dem = 22 ky tu. CHI de dem so thiet bi cho Pro (PRODUCT_POLICY 7.3).
///
/// MUST NOT suy ra tu MAC, serial, ma san pham Windows... du cong cu nay doc duoc
/// chung: du lieu quet KHONG BAO GIO la dinh danh thiet bi (ADR-0002 muc 2.3).
/// </summary>
public static class InstallationId
{
    public const int ByteLength = 16;
    public const int EncodedLength = 22;

    public static string New() => Base64Url.Encode(RandomNumberGenerator.GetBytes(ByteLength));

    /// <summary>Dung 22 ky tu base64url giai ra dung 16 byte.</summary>
    public static bool IsValid(string? value)
        => value is { Length: EncodedLength } && Base64Url.TryDecode(value) is { Length: ByteLength }
           && Base64Url.Encode(Base64Url.TryDecode(value)) == value;   // chan bit thua o ky tu cuoi
}

/// <summary>
/// Tep <c>installation-id</c> trong thu muc du lieu cuc bo.
///
/// LECH CO CHU DICH so voi 14.3 ("sinh o lan chay dau"): ADR-0002 muc 2.3 chot
/// sinh o LAN DANG NHAP DAU - ban mien phi khong dung ma, va mot cong cu ra
/// quet chay qua RMM tren hang tram may khong nen de lai tep nao khi khong ai
/// dang nhap. Doc (<see cref="TryGet"/>) KHONG BAO GIO tao ma.
/// </summary>
public sealed class InstallationIdStore(string directory)
{
    public const string FileName = "installation-id";

    public string FilePath => Path.Combine(directory, FileName);

    /// <summary>Ma da co, hoac null (chua dang nhap lan nao / tep hong). Khong ghi gi.</summary>
    public string? TryGet()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var s = File.ReadAllText(FilePath).Trim();
            return InstallationId.IsValid(s) ? s : null;
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    /// <summary>Goi khi dang nhap: tra ma cu neu hop le, khong thi sinh ma moi va luu.</summary>
    public string GetOrCreate()
    {
        if (TryGet() is { } existing) return existing;
        var id = InstallationId.New();
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, id);
        return id;
    }

    /// <summary>"Dang xuat va xoa du lieu cuc bo": lan sau sinh ma moi.</summary>
    public void Delete()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
    }
}
