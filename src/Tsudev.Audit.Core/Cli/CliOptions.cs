using Tsudev.Audit.Core.Localization;
using Tsudev.Audit.Core.Models;

namespace Tsudev.Audit.Core.Cli;

/// <summary>
/// Ma thoat va cach suy ra chung.
///
/// Nam trong Core (khong phu thuoc he dieu hanh) CO CHU DICH: day la logic
/// nghiep vu - no quyet dinh mot he thong giam sat se bao dong hay im lang.
/// Khi con nam trong lop Cli (net8.0-windows), bo test chay tren Linux khong
/// voi toi duoc, va thuc te no da KHONG duoc kiem thu dong nao - trong khi
/// README lai tuyen bo "CLI parse tham so: 16/16 test".
/// </summary>
public static class ExitCodes
{
    // Nhom SUC KHOE CONG CU - cong cu chay co tron ven khong
    public const int Ok = 0;
    public const int Partial = 1;
    public const int Fatal = 2;
    public const int BadArgs = 3;

    // Nhom KET LUAN DANH GIA - may duoc quet co van de khong
    public const int VerdictWarning = 10;
    public const int VerdictCritical = 20;

    /// <summary>
    /// Can cap nhat truoc khi quet. Chi dung o che do khong tuong tac
    /// (--silent): khi do KHONG hien hop thoai ma tra ve ma nay, de he thong
    /// trien khai tu xu ly. Hien hop thoai trong mot tien trinh tu dong se
    /// treo cung ca lan trien khai - khong ai ngoi do bam nut.
    /// </summary>
    public const int UpdateRequired = 30;

    /// <summary>
    /// Nguoi dung bam Ctrl+C.
    ///
    /// Chon 130 vi day la quy uoc POSIX da co san (128 + SIGINT), duoc bash,
    /// PowerShell va gan nhu moi he CI hieu san la "bi ngat", khong phai "chay
    /// loi". Dat mot ma rieng cua rieng minh se buoc moi he giam sat phai hoc
    /// them mot con so vo co.
    /// </summary>
    public const int Cancelled = 130;

    /// <summary>Ma thoat suy ra tu ket luan XAU NHAT trong cac bao cao da tao.</summary>
    public static int FromVerdicts(IEnumerable<VerdictLevel> verdicts)
    {
        ArgumentNullException.ThrowIfNull(verdicts);

        var list = verdicts as ICollection<VerdictLevel> ?? verdicts.ToList();
        if (list.Contains(VerdictLevel.Bad)) return VerdictCritical;
        if (list.Contains(VerdictLevel.Warning)) return VerdictWarning;
        return Ok;
    }

    /// <summary>
    /// Gop ma suc khoe cong cu voi ma ket luan danh gia.
    ///
    /// Thu tu uu tien: Fatal &gt; BadArgs &gt; VerdictCritical &gt; VerdictWarning &gt; Partial &gt; Ok.
    /// Ket luan danh gia thang "thieu du lieu" vi mot may co dau hieu kich hoat
    /// trai phep can duoc bao dong hon la mot muc thu thap bi trong.
    /// </summary>
    public static int Combine(int toolHealth, int verdict)
    {
        if (toolHealth is Fatal or BadArgs or UpdateRequired or Cancelled) return toolHealth;
        return verdict != Ok ? verdict : toolHealth;
    }

    public static string Describe(int code) => code switch
    {
        Ok => Loc.T("không phát hiện vấn đề", "no issues found"),
        Partial => Loc.T("hoàn tất nhưng thiếu dữ liệu ở một số mục", "completed, but some items are missing data"),
        Fatal => Loc.T("lỗi nghiêm trọng, không tạo được báo cáo", "fatal error, no report was produced"),
        BadArgs => Loc.T("tham số dòng lệnh không hợp lệ", "invalid command-line arguments"),
        UpdateRequired => Loc.T("cần cập nhật trước khi quét", "an update is required before scanning"),
        Cancelled => Loc.T("người dùng đã huỷ (Ctrl+C)", "cancelled by the user (Ctrl+C)"),
        VerdictWarning => Loc.T("kết luận mức CẢNH BÁO", "verdict: WARNING"),
        VerdictCritical => Loc.T("kết luận mức NGHIÊM TRỌNG", "verdict: CRITICAL"),
        _ => Loc.T("không xác định", "unknown")
    };
}

public enum AuditScope { All, License, Hardware }

/// <summary>
/// Lenh cua mot lan chay. <see cref="Scan"/> la mac dinh (hanh vi cu). Ba lenh tai khoan TsuDEV
/// (ADR-0002 muc 2.3) chi chay TUONG TAC: chung mo trinh duyet / in trang thai cho nguoi xem.
/// </summary>
public enum CliCommand { Scan, Login, Logout, Account }

public sealed class CliOptions
{
    /// <summary>Lenh: tu dau tien <c>login</c>/<c>logout</c>/<c>account</c>, khong co thi quet.</summary>
    public CliCommand Command { get; private set; } = CliCommand.Scan;

    public AuditScope Scope { get; private set; } = AuditScope.All;
    public string? OutputRoot { get; private set; }
    public bool Silent { get; private set; }
    public bool RunDism { get; private set; } = true;
    public bool RunSfc { get; private set; }
    public bool ExportCsv { get; private set; } = true;
    public bool Verbose { get; private set; }
    public bool ShowHelp { get; private set; }
    public bool ShowVersion { get; private set; }

    /// <summary>
    /// Khong cho ket luan danh gia anh huong toi ma thoat.
    /// Danh cho cac he RMM coi MOI ma khac 0 la "script chay loi" - khi do mot
    /// may co Office chua kich hoat se bi bao nham thanh su co cong cu.
    /// </summary>
    public bool NoVerdictExit { get; private set; }

    /// <summary>
    /// Bo qua hoan toan viec kiem tra phien ban moi.
    ///
    /// Danh cho: may khong noi mang, mang cam ra ngoai, va cac doi ngu tu quan
    /// ly viec cap nhat qua he thong trien khai rieng.
    /// </summary>
    public bool NoUpdateCheck { get; private set; }

    /// <summary>Duong dan file bo luat phat hien ben ngoai (tuy chon).</summary>
    public string? RulesPath { get; private set; }

    /// <summary>
    /// Ngon ngu chon bang <c>--lang</c>; null = theo he thong (hoac bien moi
    /// truong TSUSWICO_LANG). Program doc gia tri nay TRUOC khi phan tich day
    /// du qua <see cref="LanguageResolver.FindCliValue"/>, de ca thong bao loi
    /// tham so cung dung ngon ngu da chon.
    /// </summary>
    public AppLanguage? Language { get; private set; }

    /// <summary>
    /// Che do may-doc-may: stdout CHI chua mot tai lieu JSON (bao cao), moi
    /// dong tien trinh/thong bao chuyen sang stderr; khong ghi tep, khong mo
    /// trinh duyet. Danh cho RMM/script lay ket qua qua ong dan.
    /// </summary>
    public bool JsonOnly { get; private set; }

    /// <summary>
    /// Ghi nhat ky co cau truc (JSON Lines, ghi noi tiep) ra tep nay - de gui khi
    /// can ho tro tu xa. null = khong ghi. Xem <c>Tsudev.Audit.Core.Logging.RunLog</c>.
    /// </summary>
    public string? LogFile { get; private set; }

    public static CliOptions Parse(string[] args)
    {
        var o = new CliOptions();
        var start = 0;
        if (args.Length > 0)
        {
            o.Command = args[0].ToLowerInvariant() switch
            {
                "login" => CliCommand.Login,
                "logout" => CliCommand.Logout,
                "account" => CliCommand.Account,
                _ => CliCommand.Scan,
            };
            if (o.Command != CliCommand.Scan) start = 1;
        }

        for (int i = start; i < args.Length; i++)
        {
            var a = args[i].ToLowerInvariant();
            if (o.Command != CliCommand.Scan && !IsAccountOption(a))
                throw new ArgumentException(a is "--silent" or "-s" or "--json-only"
                    ? Loc.T($"'{args[0]}' chỉ chạy ở chế độ tương tác - không dùng cùng {args[i]}.",
                            $"'{args[0]}' runs interactively only - it cannot be used with {args[i]}.")
                    : Loc.T($"Tham số '{args[i]}' không dùng được với '{args[0]}'.",
                            $"Option '{args[i]}' cannot be used with '{args[0]}'."));
            switch (a)
            {
                case "-h" or "--help" or "/?": o.ShowHelp = true; break;
                case "--version": o.ShowVersion = true; break;
                case "--no-verdict-exit": o.NoVerdictExit = true; break;
                case "--no-update-check": o.NoUpdateCheck = true; break;
                case "--silent" or "-s": o.Silent = true; break;
                case "--sfc": o.RunSfc = true; break;
                case "--no-dism": o.RunDism = false; break;
                case "--no-csv": o.ExportCsv = false; break;
                case "--verbose" or "-v": o.Verbose = true; break;
                case "--json-only": o.JsonOnly = true; o.Silent = true; break;

                case "--lang":
                    if (++i >= args.Length)
                        throw new ArgumentException(Loc.T($"--lang cần một giá trị ({LanguageResolver.Choices}).",
                                                          $"--lang needs a value ({LanguageResolver.Choices})."));
                    o.Language = LanguageResolver.ParseChoice(args[i]);
                    break;

                case "--scope":
                    if (++i >= args.Length) throw new ArgumentException(Loc.T("--scope cần một giá trị (all|license|hardware).", "--scope needs a value (all|license|hardware)."));
                    o.Scope = args[i].ToLowerInvariant() switch
                    {
                        "all" => AuditScope.All,
                        "license" => AuditScope.License,
                        "hardware" => AuditScope.Hardware,
                        _ => throw new ArgumentException(Loc.T($"--scope không hợp lệ: '{args[i]}' (chỉ nhận all|license|hardware).", $"Invalid --scope: '{args[i]}' (accepted: all|license|hardware)."))
                    };
                    break;

                case "--output" or "-o":
                    if (++i >= args.Length) throw new ArgumentException(Loc.T("--output cần một đường dẫn thư mục.", "--output needs a folder path."));
                    o.OutputRoot = args[i];
                    break;

                case "--rules":
                    if (++i >= args.Length) throw new ArgumentException(Loc.T("--rules cần đường dẫn tới file .json bộ luật.", "--rules needs the path of a rules .json file."));
                    o.RulesPath = args[i];
                    break;

                case "--log-file":
                    if (++i >= args.Length || string.IsNullOrWhiteSpace(args[i]))
                        throw new ArgumentException(Loc.T("--log-file cần đường dẫn tệp nhật ký.", "--log-file needs a log file path."));
                    o.LogFile = args[i];
                    break;

                case var l when l.StartsWith("--lang=", StringComparison.Ordinal):
                    o.Language = LanguageResolver.ParseChoice(args[i]["--lang=".Length..]);
                    break;

                default:
                    throw new ArgumentException(Loc.T($"Không nhận ra tham số '{args[i]}'.", $"Unknown argument '{args[i]}'."));
            }
        }
        return o;
    }

    /// <summary>Tham so hop le sau <c>login</c>/<c>logout</c>/<c>account</c>: ngon ngu, chi tiet, nhat ky, tro giup.</summary>
    private static bool IsAccountOption(string a)
        => a is "-h" or "--help" or "/?" or "--verbose" or "-v" or "--lang" or "--log-file"
           || a.StartsWith("--lang=", StringComparison.Ordinal)
           || !a.StartsWith('-');   // gia tri cua --lang/--log-file

    public static void PrintUsage() => Console.WriteLine(UsageText);

    /// <summary>Toan van phan tro giup - tach ra de kiem thu duoc.</summary>
    public static string UsageText => Loc.IsEnglish ? UsageTextEn : UsageTextVi;

    private const string UsageTextVi = """
Tsu Swico - Kiểm tra bản quyền Windows & cấu hình phần cứng

CÁCH DÙNG:
  swico.exe [tham số]
  swico.exe login | logout | account

TÀI KHOẢN TSUDEV (KHÔNG BẮT BUỘC - bản miễn phí dùng đủ mọi chức năng quét):
  login                           Đăng nhập tsudev.com bằng trình duyệt của máy
  account                         Xem tài khoản, gói TsuDEV Pro, số thiết bị
  logout                          Đăng xuất và xoá dữ liệu tài khoản trên máy này
  Chỉ chạy ở chế độ tương tác (không dùng cùng --silent/--json-only).

THAM SỐ:
  --scope <all|license|hardware>  Phạm vi quét (mặc định: all - quét cả hai)
  -o, --output <thư mục>          Thư mục gốc lưu kết quả (mặc định: cạnh file exe)
  -s, --silent                    Không tự mở trình duyệt (dùng cho GPO/RMM/Task Scheduler)
      --sfc                       Chạy thêm sfc /verifyonly (CHẬM 5-15 phút, mặc định tắt)
      --no-dism                   Bỏ qua DISM CheckHealth
      --no-csv                    Không xuất file .csv
      --rules <file.json>         Dùng bộ luật phát hiện từ file ngoài
      --no-verdict-exit           Kết luận đánh giá KHÔNG ảnh hưởng mã thoát
      --no-update-check           Bỏ qua kiểm tra phiên bản mới
      --json-only                 Chỉ in báo cáo JSON ra stdout (không ghi tệp, không mở trình duyệt)
      --lang <auto|vi|en>         Ngôn ngữ (mặc định: auto - theo Windows; hoặc biến TSUSWICO_LANG)
      --log-file <tệp.jsonl>      Ghi nhật ký có cấu trúc (JSON Lines) để gửi khi cần hỗ trợ
  -v, --verbose                   Hiện chi tiết lỗi
      --version                   Hiện phiên bản rồi thoát
  -h, --help                      Hiện trợ giúp này

BỘ LUẬT PHÁT HIỆN:
  Các dấu hiệu kích hoạt trái phép nằm trong một file dữ liệu riêng, cập nhật
  được độc lập với file exe - không cần cài lại. Thứ tự ưu tiên:
    1. File chỉ định bằng --rules
    2. File detection-rules.json đặt cạnh swico.exe
    3. Bộ luật đóng kèm bên trong exe
  File hỏng hoặc thiếu sẽ tự quay về bộ luật đóng kèm kèm một cảnh báo,
  KHÔNG làm hỏng lần quét.

KIỂM TRA CẬP NHẬT:
  Khi khởi động, công cụ hỏi GitHub xem đã có phiên bản mới chưa. Nếu có, phải
  cập nhật trước khi quét - để kết quả dựa trên bộ luật phát hiện mới nhất.

  Khi CHƯA đăng nhập, đây là lần DUY NHẤT công cụ kết nối Internet. Không có
  dữ liệu nào của máy được gửi đi. Đăng nhập thêm các kết nối tới tsudev.com
  để lấy quyền lợi TsuDEV Pro - xem PRIVACY.md.

  Nếu KHÔNG kiểm tra được (mất mạng, tường lửa chặn), công cụ vẫn quét bình
  thường và ghi một ghi chú vào báo cáo. Chặn ở đây sẽ làm công cụ vô dụng
  đúng ở nơi cần nó nhất.

  Ở chế độ --silent, công cụ KHÔNG hiện hộp thoại mà thoát với mã 30.

MÃ THOÁT: chia hai nhóm, KHÔNG gộp chung

  Sức khoẻ công cụ - công cụ chạy có trọn vẹn không:
    0   Hoàn tất, không phát hiện vấn đề
    1   Hoàn tất nhưng thiếu dữ liệu ở một số mục (báo cáo chính vẫn đầy đủ)
    2   Lỗi nghiêm trọng, không tạo được báo cáo
    3   Tham số dòng lệnh không hợp lệ

  Kết luận đánh giá - máy được quét có vấn đề không:
    10  Kết luận mức CẢNH BÁO (ví dụ: Office chưa kích hoạt)
    20  Kết luận mức NGHIÊM TRỌNG (dấu hiệu kích hoạt trái phép)
    30  Cần cập nhật trước khi quét (chỉ ở chế độ --silent)
   130  Người dùng đã huỷ bằng Ctrl+C (quy ước POSIX 128+SIGINT)

  Thứ tự ưu tiên khi nhiều điều kiện cùng xảy ra: 2 > 3 > 130 > 20 > 10 > 1 > 0

  Hệ thống giám sát cần phân biệt "công cụ đọc thiếu dữ liệu" với "máy này có
  vấn đề bản quyền" - gộp cả hai vào một mã thì mất đúng thông tin quan trọng
  nhất. Nếu hệ RMM của bạn coi MỌI mã khác 0 là script lỗi, thêm
  --no-verdict-exit để chỉ dùng nhóm mã sức khoẻ công cụ.

VÍ DỤ:
  swico.exe                             Quét đầy đủ, mở báo cáo khi xong
  swico.exe --scope license             Chỉ kiểm tra bản quyền
  swico.exe --silent --sfc              Quét sâu, không mở trình duyệt (triển khai hàng loạt)
  swico.exe -o D:\BaoCao --silent       Lưu kết quả vào thư mục chỉ định
  swico.exe --rules .\luat-moi.json     Quét bằng bộ luật cập nhật
  swico.exe --silent --no-verdict-exit  Cho RMM coi mọi mã khác 0 là lỗi
  swico.exe --json-only > ketqua.json   Lấy kết quả JSON qua ống dẫn
""";

    private const string UsageTextEn = """
Tsu Swico - Windows license & hardware configuration audit

USAGE:
  swico.exe [options]
  swico.exe login | logout | account

TSUDEV ACCOUNT (OPTIONAL - the free edition has every scanning feature):
  login                           Sign in to tsudev.com with this computer's browser
  account                         Show the account, TsuDEV Pro plan and device count
  logout                          Sign out and delete account data on this computer
  Interactive only (cannot be combined with --silent/--json-only).

OPTIONS:
  --scope <all|license|hardware>  What to scan (default: all - both)
  -o, --output <folder>           Root folder for results (default: next to the exe)
  -s, --silent                    Do not open the browser (for GPO/RMM/Task Scheduler)
      --sfc                       Also run sfc /verifyonly (SLOW, 5-15 minutes, off by default)
      --no-dism                   Skip DISM CheckHealth
      --no-csv                    Do not export .csv files
      --rules <file.json>         Use detection rules from an external file
      --no-verdict-exit           The verdict does NOT affect the exit code
      --no-update-check           Skip the check for a newer version
      --json-only                 Print only the JSON report to stdout (no files, no browser)
      --lang <auto|vi|en>         Language (default: auto - follows Windows; or TSUSWICO_LANG)
      --log-file <file.jsonl>     Write a structured log (JSON Lines) to send for support
  -v, --verbose                   Show error details
      --version                   Show the version and exit
  -h, --help                      Show this help

DETECTION RULES:
  Signs of unauthorized activation live in a separate data file that can be
  updated independently of the exe - no reinstall needed. Priority order:
    1. The file given with --rules
    2. detection-rules.json placed next to swico.exe
    3. The rule set built into the exe
  A broken or missing file falls back to the built-in rules with a warning;
  it does NOT break the scan.

UPDATE CHECK:
  On start-up the tool asks GitHub whether a newer version exists. If so, it
  must be updated before scanning - so results use the latest detection rules.

  While NOT signed in, this is the ONLY time the tool connects to the Internet.
  No data about the machine is sent. Signing in adds connections to tsudev.com
  to fetch TsuDEV Pro entitlements - see PRIVACY.md.

  If the check FAILS (offline, firewall), the tool still scans normally and
  adds a note to the report. Blocking here would make the tool useless exactly
  where it is needed most.

  In --silent mode the tool shows NO dialog and exits with code 30.

EXIT CODES: two groups, deliberately NOT merged

  Tool health - did the tool run completely:
    0   Completed, no issues found
    1   Completed, but some items are missing data (main report still complete)
    2   Fatal error, no report was produced
    3   Invalid command-line arguments

  Verdict - does the scanned machine have a problem:
    10  Verdict WARNING (e.g. Office not activated)
    20  Verdict CRITICAL (signs of unauthorized activation)
    30  Update required before scanning (--silent mode only)
   130  Cancelled by the user with Ctrl+C (POSIX convention 128+SIGINT)

  Priority when several conditions apply: 2 > 3 > 130 > 20 > 10 > 1 > 0

  Monitoring systems need to tell "the tool read incomplete data" apart from
  "this machine has a licensing problem" - one shared code loses exactly the
  most important information. If your RMM treats EVERY non-zero code as a
  script failure, add --no-verdict-exit to use only the tool-health codes.

EXAMPLES:
  swico.exe                             Full scan, open the report when done
  swico.exe --scope license             License check only
  swico.exe --silent --sfc              Deep scan without opening a browser (mass deployment)
  swico.exe -o D:\Reports --silent      Save results to a given folder
  swico.exe --rules .\new-rules.json    Scan with updated rules
  swico.exe --silent --no-verdict-exit  Let an RMM treat every non-zero code as failure
  swico.exe --json-only > result.json   Get the JSON result through a pipe
""";
}
