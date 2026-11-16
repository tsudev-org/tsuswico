using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>
/// Liet ke phan mem da cai tu 3 nhanh registry Uninstall (64-bit, 32-bit,
/// va per-user) - day du hon nhieu so voi Win32_Product (von cham va chi thay
/// phan mem cai qua MSI).
/// </summary>
public static class SoftwareCollector
{
    private static readonly (RegistryRoot Root, string Path)[] UninstallKeys =
    {
        (RegistryRoot.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
        (RegistryRoot.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
        (RegistryRoot.CurrentUser,  @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
    };

    /// <summary>Bang tra phan loai theo tu khoa - khong day du 100%, chi ho tro doc nhanh.</summary>
    private static readonly (string Keyword, string Category)[] CategoryRules =
    {
        ("visual studio", "Công cụ lập trình"), ("vscode", "Công cụ lập trình"),
        ("git", "Công cụ lập trình"), ("python", "Công cụ lập trình"),
        ("node", "Công cụ lập trình"), ("docker", "Công cụ lập trình"),
        ("office", "Văn phòng"), ("word", "Văn phòng"), ("excel", "Văn phòng"),
        ("zoom", "Họp trực tuyến"), ("teams", "Họp trực tuyến"), ("meet", "Họp trực tuyến"),
        ("chrome", "Trình duyệt"), ("firefox", "Trình duyệt"), ("edge", "Trình duyệt"),
        ("7-zip", "Nén/giải nén"), ("winrar", "Nén/giải nén"), ("winzip", "Nén/giải nén"),
        ("photoshop", "Đồ họa"), ("illustrator", "Đồ họa"), ("gimp", "Đồ họa"),
        ("antivirus", "Bảo mật"), ("defender", "Bảo mật"), ("kaspersky", "Bảo mật"),
        ("vpn", "Mạng/VPN"), ("proxy", "Mạng/VPN"),
        ("driver", "Driver/Hệ thống"), ("runtime", "Driver/Hệ thống"), ("redistributable", "Driver/Hệ thống"),
    };

    public static List<InstalledSoftware> Collect(SystemContext ctx)
    {
        var list = new List<InstalledSoftware>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (root, basePath) in UninstallKeys)
        {
            foreach (var sub in ctx.Registry.GetSubKeyNames(root, basePath))
            {
                var keyPath = $@"{basePath}\{sub}";

                var name = Val(ctx, root, keyPath, "DisplayName");
                if (string.IsNullOrWhiteSpace(name)) continue;

                // Bo qua ban va, update, thanh phan he thong an
                var systemComponent = ctx.Registry.GetValue(root, keyPath, "SystemComponent");
                if (systemComponent is int sc && sc == 1) continue;
                if (!string.IsNullOrWhiteSpace(Val(ctx, root, keyPath, "ParentKeyName"))) continue;

                var version = Val(ctx, root, keyPath, "DisplayVersion");
                var publisher = Val(ctx, root, keyPath, "Publisher");

                // Chong trung: cung ten + cung phien ban thi chi lay 1 lan
                var dedupeKey = $"{name}|{version}";
                if (!seen.Add(dedupeKey)) continue;

                list.Add(new InstalledSoftware(
                    Name: Fallback(name),
                    Version: Fallback(version),
                    Publisher: Fallback(publisher),
                    InstallDate: FormatInstallDate(Val(ctx, root, keyPath, "InstallDate")),
                    InstallLocation: Fallback(Val(ctx, root, keyPath, "InstallLocation")),
                    Category: Classify(name),
                    ProductGuid: sub));
            }
        }

        return list.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static string Val(SystemContext ctx, RegistryRoot root, string path, string name)
        => ctx.Registry.GetValue(root, path, name)?.ToString()?.Trim() ?? "";

    private static string Fallback(string s) => string.IsNullOrWhiteSpace(s) ? "-" : s;

    /// <summary>
    /// Registry luu InstallDate dang "yyyyMMdd" -> chuyen sang DD/MM/YYYY de HIEN THI
    /// (quy uoc docs/DESIGN_SYSTEM.md, xem <see cref="DateDisplay"/>).
    /// Cat chuoi thay vi DateTime.ParseExact vi registry cua may that co chua ngay
    /// khong ton tai ("20240230") va nam 0 - ParseExact se nem, con nguoi doc thi
    /// van can nhin thay con so tho de biet may minh ghi gi.
    /// </summary>
    public static string FormatInstallDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "-";
        raw = raw.Trim();
        if (raw.Length == 8 && raw.All(char.IsDigit))
            return $"{raw.Substring(6, 2)}/{raw.Substring(4, 2)}/{raw[..4]}";
        return raw;
    }

    public static string Classify(string name)
    {
        foreach (var (kw, cat) in CategoryRules)
            if (name.Contains(kw, StringComparison.OrdinalIgnoreCase)) return cat;
        return "Chưa phân loại";
    }

    public static DataTable ToTable(IReadOnlyList<InstalledSoftware> items)
    {
        var t = DataTable.Create("", "Tên phần mềm", "Phiên bản", "Nhà phát hành",
            "Ngày cài đặt", "Thư mục cài đặt", "Phân loại", "Mã sản phẩm (GUID)");
        t.Searchable = true;
        foreach (var s in items)
            t.AddRow(s.Name, s.Version, s.Publisher, s.InstallDate, s.InstallLocation, s.Category, s.ProductGuid);
        t.AddEmptyNotice("Không đọc được phần mềm nào từ registry");
        return t;
    }

    public static DataTable ToManualReviewTable(IReadOnlyList<InstalledSoftware> items)
    {
        var t = DataTable.Create("", "Tên phần mềm", "Phiên bản", "Nhà phát hành", "Thư mục cài đặt", "Mã sản phẩm (GUID)");
        t.Searchable = true;
        foreach (var s in items.Where(x => x.NeedsManualReview))
            t.AddRow(s.Name, s.Version, s.Publisher, s.InstallLocation, s.ProductGuid);
        t.AddEmptyNotice("Không có phần mềm nào thiếu thông tin - tốt");
        return t;
    }
}
