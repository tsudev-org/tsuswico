using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Tsudev.Audit.Core.Pro;

namespace Tsudev.Audit.Windows;

/// <summary>
/// DPAPI pham vi CurrentUser (ADR-0002 muc 2.3) bang P/Invoke <c>crypt32!CryptProtectData</c>.
///
/// VI SAO KHONG dung goi NuGet <c>System.Security.Cryptography.ProtectedData</c>: no chi boc dung hai
/// ham nay; them mot goi (va mot dong lock file, mot muc THIRD-PARTY-NOTICES) cho 40 dong ma BCL lam
/// duoc la khong dang. Entropy rieng cua app: mot tien trinh khac cung tai khoan goi
/// CryptUnprotectData tren tep nay ma khong biet entropy thi khong giai duoc (khong phai bi mat that,
/// chi chan doc nham/doc hang loat).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiTokenProtector : ITokenProtector
{
    private const int CryptProtectUiForbidden = 0x1;
    private static readonly byte[] Entropy = Encoding.ASCII.GetBytes("tsudev.tsuswico.refresh-token.v1");

    public byte[] Protect(byte[] plaintext)
        => Run(plaintext, protect: true) ?? throw new Win32Exception(Marshal.GetLastWin32Error(), "CryptProtectData");

    public byte[]? Unprotect(byte[] ciphertext) => Run(ciphertext, protect: false);

    private static byte[]? Run(byte[] input, bool protect)
    {
        var inHandle = GCHandle.Alloc(input, GCHandleType.Pinned);
        var entHandle = GCHandle.Alloc(Entropy, GCHandleType.Pinned);
        var output = default(DataBlob);
        try
        {
            var inBlob = new DataBlob { Size = input.Length, Data = inHandle.AddrOfPinnedObject() };
            var entBlob = new DataBlob { Size = Entropy.Length, Data = entHandle.AddrOfPinnedObject() };
            var ok = protect
                ? CryptProtectData(ref inBlob, null, ref entBlob, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, ref output)
                : CryptUnprotectData(ref inBlob, IntPtr.Zero, ref entBlob, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, ref output);
            if (!ok) return null;
            var result = new byte[output.Size];
            Marshal.Copy(output.Data, result, 0, output.Size);
            return result;
        }
        finally
        {
            if (output.Data != IntPtr.Zero)
            {
                // Xoa ban ro trong bo nho cua he thong truoc khi tra lai.
                if (!protect) ZeroMemory(output.Data, (IntPtr)output.Size);
                LocalFree(output.Data);
            }
            inHandle.Free();
            entHandle.Free();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Size;
        public IntPtr Data;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(ref DataBlob dataIn, string? description, ref DataBlob entropy,
        IntPtr reserved, IntPtr prompt, int flags, ref DataBlob dataOut);

    [DllImport("crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(ref DataBlob dataIn, IntPtr description, ref DataBlob entropy,
        IntPtr reserved, IntPtr prompt, int flags, ref DataBlob dataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr mem);

    [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory")]
    private static extern void ZeroMemory(IntPtr dest, IntPtr size);
}

/// <summary>Mo trinh duyet he thong cho luong dang nhap.</summary>
[SupportedOSPlatform("windows")]
public static class SystemBrowser
{
    /// <summary>
    /// swico.exe chay voi quyen quan tri (app.manifest). ShellExecute mot URL tu tien trinh da nang quyen
    /// se mo trinh duyet CUNG quyen quan tri - trang web dang nhap chay elevated la dieu khong ai muon.
    /// Giao URL cho <c>explorer.exe</c>: explorer.exe dang chay (khong nang quyen) cua phien nhan lenh va
    /// mo trinh duyet mac dinh o muc quyen thuong. Chi nhan URL https (khong de ky tu la chen thanh tham so).
    /// </summary>
    public static bool Open(Uri url)
    {
        if (url.Scheme != Uri.UriSchemeHttps) return false;
        try
        {
            var explorer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            using var p = Process.Start(new ProcessStartInfo(explorer) { ArgumentList = { url.AbsoluteUri }, UseShellExecute = false });
            return p is not null;
        }
        catch (Win32Exception) { }
        catch (InvalidOperationException) { }
        try
        {
            using var p = Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
            return p is not null;
        }
        catch (Win32Exception) { return false; }
        catch (InvalidOperationException) { return false; }
    }
}
