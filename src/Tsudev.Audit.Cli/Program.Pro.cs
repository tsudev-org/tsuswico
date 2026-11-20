using System.Runtime.Versioning;
using Tsudev.Audit.Core.Cli;
using Tsudev.Audit.Core.Localization;
using Tsudev.Audit.Core.Pro;
using Tsudev.Audit.Core.Reports;
using Tsudev.Audit.Windows;

namespace Tsudev.Audit.Cli;

/// <summary>
/// Tai khoan TsuDEV va quyen loi Pro (ADR-0002 muc 2.3-2.4, PRO1 P3). Moi luot goi mang di qua
/// <see cref="ProAccount"/>; chua dang nhap thi khong co luot nao (hang C).
/// </summary>
public static partial class Program
{
    private const int ExitAccountFailed = ExitCodes.Fatal;

    [SupportedOSPlatform("windows")]
    private static ProAccount CreateProAccount(HttpClient http)
    {
        var dir = ProContract.DefaultStateDirectory();
        return new ProAccount(
            new ProAccountStore(dir, new DpapiTokenProtector()),
            new InstallationIdStore(dir),
            new ProHttp(http, ProEndpoints.Production, ProContract.ClientId),
            ProEndpoints.Production,
            ProContract.ClientId,
            () => DateTimeOffset.UtcNow);
    }

    /// <summary>Khong tu theo chuyen huong: diem cuoi da ghi cung, mot 30x la dau hieu sai, khong phai duong di.</summary>
    private static HttpClient NewProHttpClient(TimeSpan timeout)
    {
#pragma warning disable CA2000 // HttpClient so huu handler (disposeHandler mac dinh = true)
        var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = timeout };
#pragma warning restore CA2000
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TsuSwico/" + CurrentVersion());
        return http;
    }

    [SupportedOSPlatform("windows")]
    private static int RunAccountCommand(CliOptions opts)
    {
        using var cancel = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            if (cancel.IsCancellationRequested) return;
            e.Cancel = true;
            cancel.Cancel();
        };
        using var http = NewProHttpClient(TimeSpan.FromSeconds(20));
        var account = CreateProAccount(http);
        try
        {
            return opts.Command switch
            {
                CliCommand.Login => LoginCommand(account, cancel.Token),
                CliCommand.Logout => LogoutCommand(account, cancel.Token),
                _ => AccountStatusCommand(account, cancel.Token),
            };
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
            Warn(Loc.T("\nĐã huỷ theo yêu cầu.", "\nCancelled on request."));
            return ExitCodes.Cancelled;
        }
    }

    [SupportedOSPlatform("windows")]
    private static int LoginCommand(ProAccount account, CancellationToken cancel)
    {
        Info(Loc.T("Đăng nhập tài khoản TsuDEV (tsudev.com) bằng trình duyệt của máy...",
                   "Signing in to your TsuDEV account (tsudev.com) with this computer's browser..."));
        var outcome = account.LoginAsync(
            SystemBrowser.Open,
            url =>
            {
                Info(Loc.T("Nếu trình duyệt không tự mở, hãy dán liên kết này vào trình duyệt:",
                           "If the browser does not open, paste this link into a browser:"));
                // KHONG qua Info: URL mang `state` cua luot dang nhap dang mo - khong ghi vao --log-file
                // (THREAT_MODEL T10), de tien trinh doc nhat ky khong tiem duoc phan hoi gia vao cong loopback.
                Console.WriteLine("  " + url.AbsoluteUri);
                Info(Loc.T("Đang chờ đăng nhập (tối đa 5 phút, Ctrl+C để huỷ)...",
                           "Waiting for sign-in (up to 5 minutes, Ctrl+C to cancel)..."));
            },
            ProAccount.LoginTimeout, cancel).GetAwaiter().GetResult();
        Log.Info("pro.login", null, ("status", outcome.Status), ("detail", outcome.Detail));

        if (!outcome.IsSuccess)
        {
            Warn(LoginFailureText(outcome));
            return ExitAccountFailed;
        }
        Info(Loc.T("\nĐã đăng nhập.", "\nSigned in."));
        PrintAccount(outcome.Account, outcome.Entitlement, RefreshStatus.Updated);
        return ExitOk;
    }

    private static string LoginFailureText(LoginOutcome o) => o.Status switch
    {
        LoginStatus.Timeout => Loc.T("Hết 5 phút mà chưa đăng nhập xong. Chạy lại: swico login",
                                     "Sign-in did not finish within 5 minutes. Run again: swico login"),
        LoginStatus.Denied => Loc.T($"tsudev.com không cho đăng nhập ({o.Detail}). Bạn đã bấm Từ chối?",
                                    $"tsudev.com did not allow the sign-in ({o.Detail}). Did you choose Deny?"),
        LoginStatus.StateMismatch => Loc.T("Phản hồi đăng nhập không khớp phiên này - đã bỏ qua để an toàn. Chạy lại: swico login",
                                           "The sign-in response did not match this session - ignored for safety. Run again: swico login"),
        LoginStatus.WrongIssuer => Loc.T("Phản hồi đăng nhập không đến từ tsudev.com - đã bỏ qua.",
                                         "The sign-in response did not come from tsudev.com - ignored."),
        LoginStatus.TokenRejected => Loc.T($"tsudev.com từ chối mã đăng nhập ({o.Detail}). Chạy lại: swico login",
                                           $"tsudev.com rejected the sign-in code ({o.Detail}). Run again: swico login"),
        LoginStatus.IdTokenInvalid => Loc.T($"Danh tính trả về không kiểm được chữ ký ({o.Detail}) - không lưu gì.",
                                            $"The returned identity failed verification ({o.Detail}) - nothing was saved."),
        LoginStatus.NoRefreshToken => Loc.T("tsudev.com không cấp quyền giữ đăng nhập (offline_access) - không lưu gì.",
                                            "tsudev.com did not grant a persistent sign-in (offline_access) - nothing was saved."),
        LoginStatus.Offline => Loc.T($"Không kết nối được tsudev.com ({o.Detail}). Kiểm tra mạng rồi chạy lại.",
                                     $"Could not reach tsudev.com ({o.Detail}). Check the network and run again."),
        LoginStatus.NoLoopback => Loc.T($"Không mở được cổng nhận phản hồi trên 127.0.0.1 lẫn [::1] ({o.Detail}).",
                                        $"Could not open a callback port on 127.0.0.1 or [::1] ({o.Detail})."),
        _ => o.Status.ToString(),
    };

    [SupportedOSPlatform("windows")]
    private static int LogoutCommand(ProAccount account, CancellationToken cancel)
    {
        var wasSignedIn = account.IsSignedIn;
        var revoked = account.LogoutAsync(cancel).GetAwaiter().GetResult();
        Log.Info("pro.logout", null, ("was_signed_in", wasSignedIn), ("revoked", revoked));
        if (!wasSignedIn)
        {
            Info(Loc.T("Chưa đăng nhập - không có gì để đăng xuất. Đã dọn dữ liệu tài khoản cũ (nếu có) trên máy này.",
                       "Not signed in - nothing to sign out of. Leftover account data on this computer (if any) was removed."));
            return ExitOk;
        }
        Info(Loc.T("Đã đăng xuất và xoá dữ liệu tài khoản trên máy này: ", "Signed out and deleted account data on this computer: ")
             + ProContract.DefaultStateDirectory());
        if (revoked != true)
            Warn(Loc.T("Không báo được cho tsudev.com (mạng?). Phiên trên máy chủ tự hết hạn; có thể đăng xuất từ xa ở tsudev.com > Thiết bị và phiên.",
                       "Could not notify tsudev.com (network?). The server session expires on its own; you can also end it at tsudev.com > Devices and sessions."));
        Info(Loc.T("Báo cáo đã quét KHÔNG bị xoá.", "Scan reports were NOT deleted."));
        return ExitOk;
    }

    [SupportedOSPlatform("windows")]
    private static int AccountStatusCommand(ProAccount account, CancellationToken cancel)
    {
        if (!account.IsSignedIn)
        {
            // Hang C: khong goi mang, khong tao tep.
            Info(Loc.T("Chưa đăng nhập tài khoản TsuDEV.", "Not signed in to a TsuDEV account."));
            Info(Loc.T("Bản miễn phí dùng đủ mọi chức năng quét. Đăng nhập (không bắt buộc): swico login",
                       "The free edition has every scanning feature. Sign in (optional): swico login"));
            return ExitOk;
        }
        var r = account.RefreshAsync(cancel).GetAwaiter().GetResult();
        Log.Info("pro.account", null, ("status", r.Status), ("detail", r.Detail));
        if (r.Status == RefreshStatus.SessionExpired)
        {
            Warn(Loc.T("Phiên đăng nhập đã hết hạn hoặc bị thu hồi. Đăng nhập lại: swico login",
                       "The sign-in session has expired or was revoked. Sign in again: swico login"));
            return ExitOk;
        }
        PrintAccount(account.Account, r.Entitlement, r.Status);
        return ExitOk;
    }

    private static void PrintAccount(AccountInfo? info, Entitlement? ent, RefreshStatus status)
    {
        if (info is not null)
            Info(Loc.T("Tài khoản TsuDEV: ", "TsuDEV account: ") + info.Subject
                 + Loc.T("   (đăng nhập lúc ", "   (signed in at ") + DateDisplay.DateTimeText(info.SignedInAt.ToLocalTime()) + ")");

        if (status is RefreshStatus.Offline or RefreshStatus.Busy or RefreshStatus.Rejected)
            Warn(Loc.T("Không lấy được quyền lợi mới - đang dùng bản lưu trên máy (nếu còn hạn).",
                       "Could not fetch fresh entitlements - using the copy saved on this computer (if still valid)."));

        if (ent is null)
        {
            Info(Loc.T("Gói: Miễn phí (chưa có thông tin quyền lợi hợp lệ)", "Plan: Free (no valid entitlement information)"));
            return;
        }
        var now = DateTimeOffset.UtcNow;
        var isPro = ent.IsProAt(now);
        Info(isPro
            ? Loc.T("Gói: TsuDEV Pro - còn hạn đến ", "Plan: TsuDEV Pro - valid until ")
              + DateDisplay.Date(DateTimeOffset.FromUnixTimeSeconds(ent.ProUntil).ToLocalTime())
            : Loc.T("Gói: Miễn phí", "Plan: Free"));
        if (BadgeLabel(ent.Badge) is { } badge) Info(Loc.T("Huy hiệu: ", "Badge: ") + badge);
        Info(Loc.T("Thiết bị dùng tài khoản trong 30 ngày: ", "Devices using the account in the last 30 days: ")
             + ent.Devices30d.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Info(Loc.T("Thông tin quyền lợi lưu trên máy đến: ", "Entitlement copy on this computer valid until: ")
             + DateDisplay.DateTimeText(ent.ExpiresAt.ToLocalTime()));
        if (!isPro) PrintPurchase(Console.Out);
    }

    /// <summary>
    /// ADR-0002 muc 2.5: dieu khoan NGUYEN VAN PRODUCT_POLICY 7.5 dung TRUOC lien ket mua (kenh tsudev.com - ban phat
    /// hanh ngoai cua hang). Chi in khi chua co Pro. Khong qua Info: khong ghi dieu khoan vao --log-file.
    /// </summary>
    private static void PrintPurchase(TextWriter output)
    {
        output.WriteLine();
        output.WriteLine(ProRenewal.PurchaseTerms);
        output.WriteLine(Loc.T("Mua TsuDEV Pro: ", "Get TsuDEV Pro: ") + ProContract.ProUrl(ProContract.ClientId));
    }

    /// <summary>ADR-0002 muc 2.6: nhan chu theo <c>badge</c> cua JWS; khong tu ve huy hieu.</summary>
    private static string? BadgeLabel(string badge) => badge switch
    {
        "pro" => "[Pro]",
        "donor" => Loc.T("[Ủng hộ]", "[Supporter]"),
        "both" => Loc.T("[Pro] [Ủng hộ]", "[Pro] [Supporter]"),
        _ => null,
    };

    /// <summary>
    /// ProNoticePrompt o dau lan quet TUONG TAC (ADR-0002 muc 2.4). Ben goi da loai --silent/--json-only.
    /// Moi su co o day (mang, tep, DPAPI) KHONG duoc lam hong lan quet: nuot loi, ghi nhat ky, quet tiep.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static void ShowProNotice(CliOptions opts)
    {
        try
        {
            using var http = NewProHttpClient(TimeSpan.FromSeconds(5));
            var account = CreateProAccount(http);
            if (!account.IsSignedIn) return;   // hang C: khong mang, khong tep
            var source = account.ForRunAsync(interactive: true, CancellationToken.None).GetAwaiter().GetResult();
            var dir = ProContract.DefaultStateDirectory();
            // Muc 7.6 truoc: nhac gia han / bao het ky. Da hien thi thoi thong bao 7.3 o lan chay nay - mot hop thoai la du.
            var renewal = new ProRenewalPrompt(source, new LocalStateStore(dir), ProContract.ClientId, Console.Out,
                                               Console.ReadLine, url => SystemBrowser.Open(new Uri(url)))
                .Run(DateTimeOffset.UtcNow, interactive: true);
            Log.Info("pro.renewal", null, ("choice", renewal));
            if (renewal != ProRenewalChoice.NotShown) return;
            var choice = new ProNoticePrompt(source, new LocalStateStore(dir), ProContract.ClientId, Console.Out,
                                             Console.ReadLine, url => SystemBrowser.Open(new Uri(url)))
                .Run(DateTimeOffset.UtcNow, interactive: true, opts.OutputRoot ?? AppContext.BaseDirectory);
            Log.Info("pro.notice", null, ("choice", choice));
        }
#pragma warning disable CA1031 // thong bao Pro la phu - khong bao gio chan lan quet
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Log.Warn("pro.notice.error", ex.Message, ("type", ex.GetType().FullName));
        }
    }
}
