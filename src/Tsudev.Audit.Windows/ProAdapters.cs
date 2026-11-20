using System.ComponentModel;
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
    /// se mo trinh duyet CUNG quyen quan tri - trang dang nhap chay elevated la dieu khong ai muon.
    ///
    /// Cach chuan (Raymond Chen, "How can I launch an unelevated process from my elevated process"): nho
    /// CHINH explorer.exe cua desktop (khong nang quyen) goi ShellExecute qua COM ShellWindows ⇒
    /// IShellBrowser ⇒ IShellView ⇒ IShellFolderViewDual.Application.
    ///
    /// DA DO SAI tren Windows 11 that (20/11/2026, WINDOWS-VERIFICATION P2): <c>explorer.exe &lt;url&gt;</c>
    /// tu tien trinh nang quyen BO QUA URL va mo thu muc Documents. Khong lui ve ShellExecute truc tiep
    /// (trinh duyet elevated): that bai ⇒ tra false, nguoi dung dan lien ket da in.
    /// Chi nhan URL https.
    /// </summary>
    public static bool Open(Uri url)
        => url.Scheme == Uri.UriSchemeHttps && TryShellExecuteUnelevated(url.AbsoluteUri);

    /// <summary>
    /// Mo tep bao cao cuc bo (HTML) bang ung dung mac dinh, o quyen thuong. Bao cao HTML co script
    /// (bo loc, phan trang) - khong nen chay trong trinh duyet elevated. Khong duoc ⇒ ShellExecute
    /// truc tiep nhu truoc (tep do chinh cong cu vua ghi, khong phai trang web la).
    /// </summary>
    public static void OpenLocalFile(string path)
    {
        if (!File.Exists(path) || TryShellExecuteUnelevated(Path.GetFullPath(path))) return;
        try { using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Win32Exception) { }
        catch (InvalidOperationException) { }
    }

    private static bool TryShellExecuteUnelevated(string file)
    {
        try
        {
            ShellExecuteUnelevated(file);
            return true;
        }
        catch (COMException) { return false; }
        catch (InvalidCastException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) { return false; }
    }

    private static void ShellExecuteUnelevated(string file)
    {
        var type = Type.GetTypeFromCLSID(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39"), throwOnError: true)!;
        var windows = (IShellWindows)Activator.CreateInstance(type)!;
        object loc = 0;          // CSIDL_DESKTOP
        object root = null!;    // VT_EMPTY
        const int SwcDesktop = 8, SwfoNeedDispatch = 1;
        var desktop = windows.FindWindowSW(ref loc, ref root, SwcDesktop, out _, SwfoNeedDispatch)
                      ?? throw new InvalidOperationException("Khong thay cua so desktop cua explorer.");

        var sidTopLevelBrowser = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837");
        var iidShellBrowser = typeof(IShellBrowser).GUID;
        ((IServiceProvider)desktop).QueryService(ref sidTopLevelBrowser, ref iidShellBrowser, out var browserObj);
        var view = ((IShellBrowser)browserObj).QueryActiveShellView();
        var iidDispatch = new Guid("00020400-0000-0000-C000-000000000046");
        view.GetItemObject(0 /* SVGIO_BACKGROUND */, ref iidDispatch, out var folderView);

        dynamic app = ((dynamic)folderView).Application;   // IShellDispatch2
        app.ShellExecute(file, "", "", "open", 1 /* SW_SHOWNORMAL */);
    }

    // ---- Giao dien COM toi thieu (thu tu vtable PHAI dung; ham giu cho chi de dem o) ----

    [ComImport, Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    private interface IShellWindows
    {
        void get_Count(); void Item(); void _NewEnum(); void Register(); void RegisterPending();
        void Revoke(); void OnNavigate(); void OnActivated();
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object? FindWindowSW([MarshalAs(UnmanagedType.Struct)] ref object pvarLoc,
                             [MarshalAs(UnmanagedType.Struct)] ref object pvarLocRoot,
                             int swClass, out int phwnd, int swfwOptions);
    }

    [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IServiceProvider
    {
        void QueryService(ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObject);
    }

    [ComImport, Guid("000214E2-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellBrowser
    {
        void GetWindow(); void ContextSensitiveHelp(); void InsertMenusSB(); void SetMenuSB(); void RemoveMenusSB();
        void SetStatusTextSB(); void EnableModelessSB(); void TranslateAcceleratorSB(); void BrowseObject();
        void GetViewStateStream(); void GetControlWindow(); void SendControlMsg();
        IShellView QueryActiveShellView();
    }

    [ComImport, Guid("000214E3-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellView
    {
        void GetWindow(); void ContextSensitiveHelp(); void TranslateAccelerator(); void EnableModeless();
        void UIActivate(); void Refresh(); void CreateViewWindow(); void DestroyViewWindow(); void GetCurrentInfo();
        void AddPropertySheetPages(); void SaveViewState(); void SelectItem();
        void GetItemObject(uint uItem, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
    }
}
