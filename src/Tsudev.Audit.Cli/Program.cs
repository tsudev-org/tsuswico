using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tsudev.Audit.Core.Cli;
using Tsudev.Audit.Core.Localization;
using Tsudev.Audit.Core.Logging;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Rendering;
using Tsudev.Audit.Core.Reports;
using Tsudev.Audit.Core.Rules;
using Tsudev.Audit.Core.Serialization;
using Tsudev.Audit.Core.Updates;
using Tsudev.Audit.Windows;

namespace Tsudev.Audit.Cli;

/// <summary>
/// Diem vao DUY NHAT - thay the ca hai file Run-License-Audit.bat va
/// Run-Hardware-Inventory.bat bang mot chuong trinh voi tham so --scope.
/// </summary>
public static partial class Program
{
    // Ma thoat nay o Tsudev.Audit.Core.Cli.ExitCodes de kiem thu duoc tren
    // moi nen tang. Cac ten duoi day chi la but danh cho de doc.
    private const int ExitOk = ExitCodes.Ok;
    private const int ExitPartial = ExitCodes.Partial;
    private const int ExitFatal = ExitCodes.Fatal;
    private const int ExitBadArgs = ExitCodes.BadArgs;

    /// <summary>Nhat ky co cau truc - chi bat bang --log-file (docs/STATE.md 3.5 muc 3).</summary>
    private static RunLog Log = RunLog.Null;

    [SupportedOSPlatform("windows")]
    public static int Main(string[] args)
    {
        var started = Stopwatch.StartNew();
        var code = ExitFatal;
        try
        {
            code = MainCore(args);
            return code;
        }
        finally
        {
            Log.Info("run.end", null, ("exit_code", code), ("exit", ExitCodes.Describe(code)),
                     ("ms", started.ElapsedMilliseconds));
            Log.Dispose();
        }
    }

    [SupportedOSPlatform("windows")]
    private static int MainCore(string[] args)
    {
        ConfigureConsole();

        // Ngon ngu chot TRUOC moi dong in ra - ke ca thong bao loi tham so.
        // PRODUCT_POLICY muc 1: --lang > TSUSWICO_LANG > ngon ngu giao dien Windows.
        Loc.SetProcessLanguage(LanguageResolver.Resolve(
            LanguageResolver.FindCliValue(args),
            Environment.GetEnvironmentVariable(LanguageResolver.EnvVar),
            CultureInfo.CurrentUICulture));

        CliOptions opts;
        try
        {
            opts = CliOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(Loc.T($"Tham số không hợp lệ: {ex.Message}\n", $"Invalid arguments: {ex.Message}\n"));
            CliOptions.PrintUsage();
            return ExitBadArgs;
        }

        if (opts.ShowHelp) { CliOptions.PrintUsage(); return ExitOk; }

        if (opts.ShowVersion) { PrintVersion(); return ExitOk; }

        OpenLog(opts, args);

        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine(Loc.T("Công cụ này chỉ chạy trên Windows.", "This tool only runs on Windows."));
            return ExitFatal;
        }

        // login/logout/account: khong quet, khong kiem tra cap nhat (ADR-0002 muc 2.3).
        if (opts.Command != CliCommand.Scan) return RunAccountCommand(opts);

        // --json-only: stdout danh rieng cho tai lieu JSON. Moi dong thong bao,
        // tien trinh, canh bao di sang stderr - nho vay `swico --json-only > a.json`
        // luon ra mot tep JSON hop le, con nguoi van thay tien trinh tren man hinh.
        TextWriter? jsonOut = null;
        if (opts.JsonOnly)
        {
            jsonOut = Console.Out;
            Console.SetOut(Console.Error);
        }

        // Kiem tra cap nhat TRUOC khi quet. Dat o day, ngoai Run(), de ro rang
        // day la mot cong doan rieng va de doc lai khi ra soat bao mat.
        var gate = CheckForUpdate(opts);
        if (gate is not null) return gate.Value;

        // Thong bao "nhieu thiet bi chua co Pro" (ADR-0002 muc 2.4) - cung cho voi hop thoai cap nhat,
        // TRUOC khi quet. --silent/--json-only (RMM/GPO, khong ai xem) MUST NOT hien; chua dang nhap
        // thi ShowProNotice tra ve ngay, khong mang, khong tep.
        if (!opts.Silent && !opts.JsonOnly) ShowProNotice(opts);

        using var cancel = new CancellationTokenSource();
        using var progress = new ConsoleProgressReporter();
        HookCancelKey(cancel, progress);

        try
        {
            return Run(opts, new LoggingProgressSink(progress, Log), jsonOut, cancel.Token);
        }
        catch (OperationCanceledException)
        {
            // Huy KHONG phai loi. Phan da quet xong van con nguyen tren man
            // hinh - do la ca diem cua viec in tung buoc mot.
            progress.Dispose();
            Warn(Loc.T("\nĐã huỷ theo yêu cầu. Những mục đã quét xong vẫn hiển thị ở trên;",
                       "\nCancelled on request. Items already scanned are still shown above;"));
            Warn(Loc.T("file báo cáo của lần quét này (nếu đã tạo) sẽ thiếu các mục còn lại.",
                       "the report files of this scan (if created) will lack the remaining items."));
            Info(Loc.T("\nMã thoát: ", "\nExit code: ") + $"{ExitCodes.Cancelled} ({ExitCodes.Describe(ExitCodes.Cancelled)})");
            return ExitCodes.Cancelled;
        }
        catch (Exception ex)
        {
            // Don dong con quay do dang TRUOC khi in loi, neu khong thong bao
            // loi se de len chinh dong dang quay va doc thanh mot dong lai cai.
            progress.Dispose();
            Log.Error("run.fatal", ex.ToString(), ("type", ex.GetType().FullName));
            Error(Loc.T("\nLỖI NGHIÊM TRỌNG: ", "\nFATAL ERROR: ") + ex.Message);
            if (opts.Verbose) Error(ex.ToString());
            return ExitFatal;
        }
    }

    /// <summary>
    /// Bat Ctrl+C de tu don dep thay vi de .NET giet ngang tien trinh.
    ///
    /// Vi sao phai tu don: mac dinh .NET ket thuc tien trinh ngay lap tuc. Khi
    /// do (1) DISM/sfc dang chay o tien trinh con van chay tiep - may van gong
    /// du terminal da tra ve dau nhac; (2) con tro dang bi an de phuc vu con
    /// quay se KHONG duoc hien lai, va phien terminal cua nguoi dung hong cho
    /// den khi ho tu go `reset`.
    ///
    /// Bam Ctrl+C LAN THU HAI thi de he dieu hanh giet that - phong khi chinh
    /// buoc don dep bi ket.
    /// </summary>
    private static void HookCancelKey(CancellationTokenSource cancel, ConsoleProgressReporter progress)
    {
        Console.CancelKeyPress += (_, e) =>
        {
            if (cancel.IsCancellationRequested) return;   // lan hai: khong chan nua

            e.Cancel = true;
            cancel.Cancel();
            progress.Dispose();
            Warn(Loc.T("\nĐang huỷ, chờ dọn dẹp... (bấm Ctrl+C lần nữa để thoát ngay)",
                       "\nCancelling, cleaning up... (press Ctrl+C again to quit immediately)"));
        };
    }

    /// <summary>
    /// Cong kiem tra phien ban. Tra ve ma thoat khi phai dung lai, null khi
    /// duoc phep quet tiep.
    ///
    /// BON TINH HUONG, BON CACH XU LY KHAC NHAU:
    ///
    ///  1. Co ban moi + ban DA CAI + chay tuong tac -> hien hop thoai mot nut,
    ///     cap nhat xong roi thoat. Nguoi dung se chay lai ban moi.
    ///  2. Co ban moi + dang chay --silent  -> KHONG hien hop thoai (se treo
    ///     cung tien trinh trien khai tu dong), thoat voi ma 30.
    ///  3. Co ban moi + ban PORTABLE -> chan lai nhung KHONG hien hop thoai va
    ///     KHONG chay file setup; chi ro cho tai ban portable moi. Chay file
    ///     setup o day se cai mot ban thu hai vao Program Files trong khi file
    ///     dang chay VAN CU - nguoi dung se bi chan mai mai.
    ///  4. Khong kiem tra duoc            -> QUET TIEP nhu binh thuong.
    ///     Cong cu nay duoc dung dung o nhung noi mang bi han che nhat; chan
    ///     lai se lam no vo dung chinh o do.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static int? CheckForUpdate(CliOptions opts)
    {
        if (opts.NoUpdateCheck)
        {
            Info(Loc.T("Đã bỏ qua kiểm tra phiên bản mới (--no-update-check).",
                       "Skipped the check for a newer version (--no-update-check)."));
            return null;
        }

        var current = CurrentVersion();
        Info(Loc.T("Đang kiểm tra phiên bản mới...", "Checking for a newer version..."));

        // Ban dang chay den tu dau quyet dinh cach cap nhat - xem tinh huong 3.
        var kind = InstallKindDetector.Detect();
        var result = new UpdateChecker(new GitHubUpdateFeed(current)).Check(current, kind);
        Log.Info("update.check", null, ("status", result.Status), ("install_kind", kind),
                 ("current", current), ("latest", result.Latest?.Version.ToString()));

        switch (result.Status)
        {
            case UpdateStatus.UpToDate:
                Info($"  {result.Message}");
                return null;

            case UpdateStatus.CheckFailed:
                Warn(Loc.T("  Không kiểm tra được bản cập nhật: ", "  Could not check for updates: ") + result.Message);
                Warn(Loc.T("  Vẫn tiếp tục quét. Hãy tự đối chiếu phiên bản mới nhất khi có mạng.",
                           "  Scanning anyway. Please check for the latest version yourself when online."));
                return null;

            case UpdateStatus.UpdateRequired:
                return HandleUpdateRequired(opts, result);

            default:
                return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static int HandleUpdateRequired(CliOptions opts, UpdateCheckResult result)
    {
        Warn($"\n=== {result.Message} ===");

        if (opts.Silent)
        {
            // Khong bao gio hien hop thoai o che do tu dong: khong ai ngoi do
            // bam nut, va tien trinh trien khai se treo vo thoi han.
            Warn(Loc.T("Chế độ --silent: không hiện hộp thoại. Cập nhật rồi chạy lại.",
                       "--silent mode: no dialog shown. Update, then run again."));
            Warn(Loc.T("Tải bản mới tại: ", "Download the new version at: ") + result.Latest?.PageUrl);
            return UpdateBlocked();
        }

        // BAN PORTABLE: khong co gi de "bam Cap nhat" ca. Hien mot hop thoai
        // mot nut o day la noi doi voi nguoi dung - bam vao khong cai duoc gi.
        if (result.MustUpdateManually)
        {
            Warn(Loc.T("Bản portable không tự cập nhật được. Hãy làm thủ công:",
                       "The portable edition cannot update itself. Please do it manually:"));
            Warn(Loc.T("  1. Tải: ", "  1. Download: ") + (result.Latest?.PortableUrl ?? result.Latest?.PageUrl));
            Warn(Loc.T("  2. Giải nén rồi ghi đè lên thư mục đang chạy công cụ này.",
                       "  2. Extract it over the folder this tool runs from."));
            Warn(Loc.T("  3. Đối chiếu SHA-256 với SHA256SUMS.txt của cùng bản phát hành đó.",
                       "  3. Verify the SHA-256 against SHA256SUMS.txt of the same release."));
            return UpdateBlocked();
        }

        if (!UpdatePrompt.AskToUpdate(result))
        {
            Warn(Loc.T("Bạn đã đóng hộp thoại mà không cập nhật. Không thể tiếp tục quét.",
                       "You closed the dialog without updating. Scanning cannot continue."));
            Warn(Loc.T("Tải bản mới tại: ", "Download the new version at: ") + result.Latest?.PageUrl);
            return UpdateBlocked();
        }

        var installer = UpdateInstaller.DownloadAndVerify(result.Latest!, Info, out var failure);
        if (installer is null)
        {
            Warn(Loc.T("Cập nhật KHÔNG thành công: ", "Update FAILED: ") + failure);
            Warn(Loc.T("Vui lòng tải và cài thủ công tại: ", "Please download and install manually from: ") + result.Latest?.PageUrl);
            return UpdateBlocked();
        }

        if (!UpdateInstaller.Launch(installer, out var launchError))
        {
            Warn(Loc.T("Không chạy được file cài đặt: ", "Could not start the installer: ") + launchError);
            Warn(Loc.T("File đã tải và đã xác minh nằm tại: ", "The downloaded, verified file is at: ") + installer);
            return UpdateBlocked();
        }

        Info(Loc.T("\nĐã khởi động trình cài đặt. Sau khi cài xong, hãy chạy lại công cụ.",
                   "\nThe installer has started. Run the tool again once it finishes."));
        return UpdateBlocked();
    }

    /// <summary>
    /// In ma thoat roi tra ve 30. Moi nhanh "phai cap nhat" deu di qua day, de
    /// khong con nhanh nao thoat lang le - nguoi doc log cua mot lan chay tu
    /// dong can nhin thay ma thoat, khong phai doan.
    /// </summary>
    private static int UpdateBlocked()
    {
        Info(Loc.T("\nMã thoát: ", "\nExit code: ") + $"{ExitCodes.UpdateRequired} ({ExitCodes.Describe(ExitCodes.UpdateRequired)})");
        return ExitCodes.UpdateRequired;
    }

    /// <summary>Phien ban hien tai, da cat phan bam commit do SDK them vao.</summary>
    private static string CurrentVersion()
    {
        var asm = typeof(Program).Assembly;
        var v = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? asm.GetName().Version?.ToString() ?? "";
        var plus = v.IndexOf('+', StringComparison.Ordinal);
        return plus > 0 ? v[..plus] : v;
    }

    [SupportedOSPlatform("windows")]
    private static int Run(CliOptions opts, LoggingProgressSink progress, TextWriter? jsonOut,
        CancellationToken cancel)
    {
        var exitCode = ExitOk;
        var scanTime = DateTimeOffset.Now;

        var root = opts.OutputRoot ?? AppContext.BaseDirectory;
        var computerName = Environment.MachineName;

        // Cau truc 3 cap: <root>/tsudev-bao-cao-ra-quet-<May>-<Ngay>/tsudev-ket-qua-ra-quet-<May>-<Ngay>/
        var level2 = FileNaming.Level2(root, computerName, scanTime);
        var level3 = FileNaming.Level3(level2, computerName, scanTime);
        var writeFiles = jsonOut is null;
        if (writeFiles) Directory.CreateDirectory(level3);

        Info($"=== TsuDEV System Audit ===");
        Info(Loc.T("Máy: ", "Computer: ") + computerName + "   |   " + Loc.T("Thời gian: ", "Time: ")
             + DateDisplay.DateTimeText(scanTime));
        if (writeFiles) Info(Loc.T("Thư mục kết quả: ", "Results folder: ") + level3);

        if (!WindowsEnvironment.IsElevated())
            Warn(Loc.T("CẢNH BÁO: đang chạy KHÔNG có quyền Administrator - một số mục sẽ thiếu dữ liệu.",
                       "WARNING: running WITHOUT Administrator rights - some items will lack data."));

        var renderer = new HtmlReportRenderer();
        var xlsx = new XlsxWriter();

        // Uu tien: --rules -> file canh exe -> bo luat dong kem.
        // Nho vay cap nhat luat chi can tha them mot file JSON canh exe, khong
        // phai bien dich lai va ky lai.
        var rulesPath = opts.RulesPath ?? ProbeConventionalRulesFile();
        var rules = DetectionRuleSet.LoadOrEmbedded(rulesPath, out var rulesWarning);
        if (rulesWarning is not null) Warn(Loc.T("CẢNH BÁO: ", "WARNING: ") + rulesWarning);
        Info(Loc.T("Bộ luật phát hiện: ", "Detection rules: ") + rules.Describe());

        var auditOptions = new AuditOptions
        {
            RunDism = opts.RunDism,
            RunSfc = opts.RunSfc,
            Rules = rules
        };

        var produced = new List<AuditReport>();

        if (opts.Scope is AuditScope.License or AuditScope.All)
        {
            Info(Loc.T("\n[1] Đang kiểm tra bản quyền Windows & phần mềm...", "\n[1] Checking Windows & software licenses..."));
            var ctx = WindowsEnvironment.CreateContext(scanTime, progress, cancel);
            var report = LicenseReportBuilder.Build(ctx, auditOptions);
            if (writeFiles && !SaveWithProgress(report, level3, renderer, xlsx, opts, progress)) exitCode = ExitPartial;
            if (report.Warnings.Count > 0) exitCode = ExitPartial;
            produced.Add(report);
            LogReport("license", report);
        }

        if (opts.Scope is AuditScope.Hardware or AuditScope.All)
        {
            Info(Loc.T("\n[2] Đang thu thập cấu hình phần cứng...", "\n[2] Collecting hardware configuration..."));
            var ctx = WindowsEnvironment.CreateContext(scanTime.AddSeconds(1), progress, cancel);
            var report = HardwareReportBuilder.Build(ctx, auditOptions);
            if (writeFiles && !SaveWithProgress(report, level3, renderer, xlsx, opts, progress)) exitCode = ExitPartial;
            if (report.Warnings.Count > 0) exitCode = ExitPartial;
            produced.Add(report);
            LogReport("hardware", report);
        }

        string? dashboardPath = null;
        if (writeFiles)
        {
            // Trang tong hop - doc SAU vao moi thu muc con cap 3 (ke ca thu muc
            // ket qua copy tu may khac vao)
            Info(Loc.T("\n[3] Đang cập nhật tsudev-tong-hop.html...", "\n[3] Updating tsudev-tong-hop.html..."));
            cancel.ThrowIfCancellationRequested();
            progress.BeginStep(Loc.T("Trang tổng hợp", "Summary page"));
            try
            {
                dashboardPath = new DashboardBuilder(renderer).BuildAndSave(level2);
                progress.EndStep();
                Info($"    → {dashboardPath}");
            }
            catch (Exception ex)
            {
                progress.FailStep(ex.Message);
                Warn(Loc.T("Không tạo được trang tổng hợp: ", "Could not create the summary page: ") + ex.Message);
                exitCode = ExitPartial;
            }
        }

        Info(Loc.T("\n=== HOÀN TẤT ===", "\n=== DONE ==="));
        foreach (var r in produced)
        {
            Info($"  {r.Title}");
            if (writeFiles) Info($"    {Path.Combine(level3, r.HtmlReportFile)}");
            if (r.Warnings.Count > 0)
                foreach (var w in r.Warnings) Warn($"    ! {w}");
        }

        if (jsonOut is not null)
        {
            jsonOut.Write(JsonOnlyDocument(produced));
            jsonOut.WriteLine();
            jsonOut.Flush();
        }
        else if (!opts.Silent && dashboardPath is not null)
            TryOpen(dashboardPath);
        else if (opts.Silent)
            Info(Loc.T("[Chế độ --silent] Bỏ qua tự động mở trình duyệt.", "[--silent mode] Not opening the browser."));

        // Ket luan danh gia thang "thieu du lieu": mot may co dau hieu kich hoat
        // trai phep can duoc bao dong hon la mot muc thu thap bi trong.
        var verdictCode = opts.NoVerdictExit
            ? ExitCodes.Ok
            : ExitCodes.FromVerdicts(produced.Select(r => r.VerdictLevel));
        exitCode = ExitCodes.Combine(exitCode, verdictCode);

        Info(Loc.T("\nMã thoát: ", "\nExit code: ") + $"{exitCode} ({ExitCodes.Describe(exitCode)})");
        return exitCode;
    }

    /// <summary>
    /// Tai lieu JSON cua che do --json-only: MOT doi tuong, luon cung hinh dang
    /// du quet mot hay hai pham vi - may doc khong phai doan "mang hay doi tuong".
    /// </summary>
    private static string JsonOnlyDocument(IEnumerable<AuditReport> reports)
    {
        var arr = new System.Text.Json.Nodes.JsonArray();
        foreach (var r in reports)
            arr.Add(System.Text.Json.Nodes.JsonNode.Parse(AuditJson.WriteReport(r)));
        var doc = new System.Text.Json.Nodes.JsonObject
        {
            ["tool"] = "Tsu Swico",
            ["version"] = CurrentVersion(),
            ["language"] = Loc.HtmlLang,
            ["reports"] = arr
        };
        return doc.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    /// <summary>
    /// Ghi bao cao ra dia, co bao tien trinh.
    ///
    /// Tach khoi <see cref="Save"/> de chinh ham ghi file khong phai biet gi ve
    /// giao dien - va de buoc ghi hien ra man hinh nhu moi buoc khac, vi voi
    /// bao cao nhieu nghin dong phan mem thi ghi XLSX khong he tuc thi.
    /// </summary>
    private static bool SaveWithProgress(AuditReport report, string dir, HtmlReportRenderer renderer,
        XlsxWriter xlsx, CliOptions opts, LoggingProgressSink progress)
    {
        progress.BeginStep(Loc.T("Ghi báo cáo ra đĩa", "Writing reports to disk"));
        try
        {
            var ok = Save(report, dir, renderer, xlsx, opts);
            progress.EndStep();
            return ok;
        }
        catch (Exception ex)
        {
            progress.FailStep(ex.Message);
            throw;
        }
    }

    private static bool Save(AuditReport report, string dir, HtmlReportRenderer renderer,
        XlsxWriter xlsx, CliOptions opts)
    {
        bool allOk = true;
        var utf8NoBom = new UTF8Encoding(false);

        try
        {
            var html = renderer.Render(report, dashboardRelativeLink: "../tsudev-tong-hop.html");
            File.WriteAllText(Path.Combine(dir, report.HtmlReportFile), html, utf8NoBom);
        }
        catch (Exception ex) { Warn(Loc.T("Không ghi được HTML: ", "Could not write HTML: ") + ex.Message); allOk = false; }

        try
        {
            var jsonName = Path.ChangeExtension(report.HtmlReportFile, ".json");
            File.WriteAllText(Path.Combine(dir, jsonName), AuditJson.WriteReport(report), utf8NoBom);
        }
        catch (Exception ex) { Warn(Loc.T("Không ghi được JSON: ", "Could not write JSON: ") + ex.Message); allOk = false; }

        try
        {
            var tables = report.Sections.SelectMany(s => s.Tables)
                .Select((t, i) => { if (string.IsNullOrWhiteSpace(t.Title)) t.Title = Loc.T("Bảng ", "Table ") + (i + 1); return t; })
                .ToList();
            xlsx.Write(Path.Combine(dir, Path.ChangeExtension(report.HtmlReportFile, ".xlsx")), tables);
        }
        catch (Exception ex) { Warn(Loc.T("Không ghi được XLSX: ", "Could not write XLSX: ") + ex.Message); allOk = false; }

        if (opts.ExportCsv)
        {
            try
            {
                foreach (var (table, idx) in report.Sections.SelectMany(s => s.Tables).Select((t, i) => (t, i)))
                {
                    if (table.Rows.Count == 0) continue;
                    var csvName = Path.ChangeExtension(report.HtmlReportFile, null) + $"_{idx + 1}.csv";
                    WriteCsv(Path.Combine(dir, csvName), table);
                }
            }
            catch (Exception ex) { Warn(Loc.T("Không ghi được CSV: ", "Could not write CSV: ") + ex.Message); allOk = false; }
        }

        return allOk;
    }

    /// <summary>Ghi CSV voi BOM de Excel mo truc tiep hien dung tieng Viet co dau.</summary>
    private static void WriteCsv(string path, DataTable table)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", table.Columns.Select(Escape)));
        foreach (var row in table.Rows)
            sb.AppendLine(string.Join(",", row.Select(Escape)));
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));

        static string Escape(string v)
            => v.Contains(',') || v.Contains('"') || v.Contains('\n')
                ? $"\"{v.Replace("\"", "\"\"")}\""
                : v;
    }

    /// <summary>
    /// Tim file luat dat canh file exe theo quy uoc. Tra ve null neu khong co -
    /// khi do dung bo luat dong kem.
    /// </summary>
    private static string? ProbeConventionalRulesFile()
    {
        try
        {
            var candidate = Path.Combine(AppContext.BaseDirectory, DetectionRuleSet.ConventionalFileName);
            return File.Exists(candidate) ? candidate : null;
        }
        catch (IOException) { return null; }
    }

    private static void PrintVersion()
    {
        var asm = typeof(Program).Assembly;
        var version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? asm.GetName().Version?.ToString()
                      ?? Loc.T("không xác định", "unknown");

        // Cat phan bam commit ma SDK tu them (vi du "3.0.0+9e1f2c...").
        var plus = version.IndexOf('+', StringComparison.Ordinal);
        if (plus > 0) version = version[..plus];

        Console.WriteLine($"Tsu Swico {version}");
        Console.WriteLine(Loc.T("Lược đồ dữ liệu: ", "Data schema: ") + AuditReport.CurrentSchemaVersion);
        Console.WriteLine(Loc.T("Bộ luật phát hiện đóng kèm: ", "Built-in detection rules: ") + DetectionRuleSet.Embedded.Version);
    }

    private static void TryOpen(string path)
    {
        // Mo o quyen THUONG qua explorer cua desktop (swico chay elevated) - xem SystemBrowser.
        try { SystemBrowser.OpenLocalFile(path); }
        catch { /* khong mo duoc trinh duyet KHONG phai loi nghiem trong */ }
    }

    /// <summary>
    /// Chuan bi console MA KHONG lam hong phien lam viec cua nguoi dung.
    ///
    /// Gan Console.OutputEncoding se goi SetConsoleOutputCP xuong Windows, va
    /// conhost/Windows Terminal DUNG LAI screen buffer khi ma trang doi - lam
    /// toan bo lich su cuon truoc do bi mat mau, trong nhu bi xoa man hinh.
    /// Nguoi dung da bao cao dung hien tuong nay.
    ///
    /// Nen chi gan KHI THAT SU CAN. Windows Terminal, PowerShell 7 va phan lon
    /// moi truong hien dai da o UTF-8 san, nen nhanh nay thuong khong chay.
    /// </summary>
    private static void ConfigureConsole()
    {
        try
        {
            if (Console.IsOutputRedirected) return;                 // ghi ra file/ong dan: khong dong toi console
            if (Console.OutputEncoding.CodePage == Encoding.UTF8.CodePage) return;  // da dung roi

            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException) { /* console khong cho doi ma trang - khong phai loi */ }
        catch (System.Security.SecurityException) { /* bi chinh sach chan */ }
    }

    /// <summary>
    /// Mo nhat ky neu co --log-file. Mo khong duoc ⇒ canh bao roi QUET TIEP:
    /// tep log la phu tro ho tro, khong dang lam hong mot lan quet.
    /// </summary>
    private static void OpenLog(CliOptions opts, string[] args)
    {
        if (opts.LogFile is null) return;
        try
        {
            Log = RunLog.OpenFile(opts.LogFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
                                       or NotSupportedException or System.Security.SecurityException)
        {
            Warn(Loc.T("Không mở được tệp nhật ký: ", "Could not open the log file: ") + ex.Message);
            return;
        }
        Log.Info("run.start", null,
            ("tool", "Tsu Swico"), ("version", CurrentVersion()), ("args", string.Join(' ', args)),
            ("lang", Loc.HtmlLang), ("scope", opts.Scope), ("silent", opts.Silent), ("json_only", opts.JsonOnly),
            ("elevated", OperatingSystem.IsWindows() && WindowsEnvironment.IsElevated()),
            ("os", Environment.OSVersion.VersionString), ("runtime", Environment.Version.ToString()));
    }

    private static void LogReport(string kind, AuditReport report)
    {
        Log.Info("report.done", null, ("kind", kind), ("verdict", report.VerdictLevel),
                 ("warnings", report.Warnings.Count));
        foreach (var w in report.Warnings) Log.Warn("report.warning", w, ("kind", kind));
    }

    private static void Info(string msg)
    {
        Log.Info("console", msg.Trim());
        Console.WriteLine(msg);
    }

    /// <summary>
    /// Loi nghiem trong: mau DO va ra stderr.
    ///
    /// Mau do phan biet han voi mau vang cua canh bao va mau thuong cua noi
    /// dung bao cao - de nguoi dung nhin mot cai la biet dong nao la ket qua,
    /// dong nao la su co. Ra stderr de dua qua ong dan van tach duoc.
    /// </summary>
    private static void Error(string msg)
    {
        Log.Error("console", msg.Trim());
        if (Console.IsErrorRedirected) { Console.Error.WriteLine(msg); return; }

        var prev = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(msg);
        }
        finally
        {
            Console.ForegroundColor = prev;
        }
        Console.Error.Flush();
    }

    private static void Warn(string msg)
    {
        Log.Warn("console", msg.Trim());
        // Khi dau ra bi chuyen huong (file log, ong dan), KHONG dat mau: ma mau
        // se lot vao file lam ban noi dung.
        if (Console.IsOutputRedirected) { Console.WriteLine(msg); return; }

        var prev = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
        }
        finally
        {
            // try/finally de mot ngoai le khi ghi KHONG de lai console mac ket
            // o mau vang sau khi chuong trinh ket thuc.
            Console.ForegroundColor = prev;
        }
    }
}
