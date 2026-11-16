using System.Text.RegularExpressions;
using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>
/// Ban quyen Microsoft Office/M365 qua cong cu chinh thuc ospp.vbs.
/// Neu may khong cai Office -> tra ve bang rong (KHONG phai loi).
/// </summary>
public static class OfficeLicenseCollector
{
    /// <summary>ApplicationID cua Microsoft Office trong SoftwareLicensingProduct.</summary>
    public const string OfficeApplicationId = "0ff1ce15-a989-479d-af46-f275c6370663";

    /// <summary>
    /// Cac vi tri co the co ospp.vbs.
    ///
    /// LUU Y: ban Click-to-Run (moi ban Office 2016 tro di, gom ca cac ban le
    /// nhu Office 16 HomePremR) cai vao thu muc "...\Microsoft Office\root\Office16",
    /// KHONG phai "...\Microsoft Office\Office16". Thieu cac duong dan "root"
    /// nay la ly do Office cai san tren may hang thuong bi bao la "khong tim
    /// thay Office" - mot diem mu that su, khong phai truong hop hiem.
    /// </summary>
    private static readonly string[] OsppCandidates =
    {
        @"%ProgramFiles%\Microsoft Office\root\Office16\ospp.vbs",
        @"%ProgramFiles(x86)%\Microsoft Office\root\Office16\ospp.vbs",
        @"%ProgramFiles%\Microsoft Office\Office16\ospp.vbs",
        @"%ProgramFiles%\Microsoft Office\Office15\ospp.vbs",
        @"%ProgramFiles(x86)%\Microsoft Office\Office16\ospp.vbs",
        @"%ProgramFiles(x86)%\Microsoft Office\Office15\ospp.vbs",
    };

    public static (DataTable Table, OfficeLicenseSummary Summary) Collect(SystemContext ctx)
    {
        var t = DataTable.Create("", "Sản phẩm", "Mô tả", "Trạng thái", "SKU ID", "Nguồn dữ liệu");
        int ok = 0, problem = 0, grace = 0, unknown = 0;

        void Count(LicenseHealth h)
        {
            switch (h)
            {
                case LicenseHealth.Ok: ok++; break;
                case LicenseHealth.Problem: problem++; break;
                case LicenseHealth.Grace: grace++; break;
                default: unknown++; break;
            }
        }

        // NGUON 1 - WMI SoftwareLicensingProduct.
        // Doc truoc vi day la nguon dang tin nhat: khong phu thuoc vao viec
        // tim thay ospp.vbs, va cac ban Office le/Click-to-Run deu dang ky o day.
        var wmiRows = ctx.Wmi.Query("SoftwareLicensingProduct",
            $"PartialProductKey IS NOT NULL AND ApplicationID='{OfficeApplicationId}'");

        foreach (var r in wmiRows)
        {
            var status = r.Num("LicenseStatus");
            Count(WindowsLicenseCollector.Classify(status));
            t.AddRow(
                r.Str("Name"),
                r.Str("Description"),
                WindowsLicenseCollector.DescribeStatus(status),
                r.Str("ID"),
                "WMI SoftwareLicensingProduct");
        }

        // NGUON 2 - ospp.vbs, cong cu chinh thuc di kem bo cai Office.
        // Bo sung cho nguon 1: co the hien thi them san pham ma WMI khong bat.
        string? ospp = null;
        foreach (var candidate in OsppCandidates)
        {
            var expanded = ctx.Files.ExpandEnvironment(candidate);
            if (ctx.Files.FileExists(expanded)) { ospp = expanded; break; }
        }

        if (ospp is not null)
        {
            var result = ctx.Process.Run("cscript.exe", $"//Nologo \"{ospp}\" /dstatus",
                timeoutSeconds: 90, cancellation: ctx.Cancellation);
            if (!result.Success && string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                ctx.Warn("Không chạy được ospp.vbs để kiểm tra bản quyền Office.");
            }
            else
            {
                foreach (var item in ParseOsppOutput(result.StandardOutput))
                {
                    Count(ClassifyOspp(item.Status));
                    t.AddRow(item.Name, item.Description, item.Status, item.SkuId, "ospp.vbs");
                }
            }
        }
        else if (wmiRows.Count > 0)
        {
            // Co du lieu WMI nhung khong tim thay ospp.vbs: KHONG duoc ket luan
            // la may khong cai Office.
            ctx.Warn("Không tìm thấy ospp.vbs (thường gặp với bản Office Click-to-Run). " +
                     "Trạng thái Office lấy từ WMI.");
        }

        t.AddEmptyNotice("Không tìm thấy Office/M365 cài đặt trên máy (đây KHÔNG phải lỗi)");
        return (t, new OfficeLicenseSummary(ok + problem + grace + unknown, ok, problem, grace, unknown));
    }

    /// <summary>Xep chuoi trang thai cua ospp.vbs vao nhom suc khoe license.</summary>
    public static LicenseHealth ClassifyOspp(string? describedStatus)
    {
        var s = (describedStatus ?? "").ToUpperInvariant();
        if (s.Contains("NON-GENUINE", StringComparison.Ordinal)
            || s.Contains("NOTIFICATION", StringComparison.Ordinal)
            || s.Contains("UNLICENSED", StringComparison.Ordinal)) return LicenseHealth.Problem;
        if (s.Contains("GRACE", StringComparison.Ordinal)) return LicenseHealth.Grace;
        if (s.Contains("LICENSED", StringComparison.Ordinal)) return LicenseHealth.Ok;
        return LicenseHealth.Unknown;
    }

    public sealed record OfficeLicenseItem(string Name, string Description, string Status, string SkuId);

    /// <summary>
    /// Phan tich output cua "ospp.vbs /dstatus". Tach rieng thanh ham public
    /// de UNIT TEST duoc bang du lieu mau, khong can cai Office.
    /// </summary>
    public static List<OfficeLicenseItem> ParseOsppOutput(string raw)
    {
        var items = new List<OfficeLicenseItem>();
        if (string.IsNullOrWhiteSpace(raw)) return items;

        // Cac khoi san pham cach nhau bang duong ke "-----"
        var blocks = Regex.Split(raw, @"-{5,}");
        foreach (var block in blocks)
        {
            if (!block.Contains("LICENSE NAME", StringComparison.OrdinalIgnoreCase)) continue;

            var name = Match1(block, @"LICENSE NAME:\s*(.+)");
            var desc = Match1(block, @"LICENSE DESCRIPTION:\s*(.+)");
            var sku = Match1(block, @"SKU ID:\s*(\S+)");
            var statusRaw = Match1(block, @"LICENSE STATUS:\s*-*\s*([A-Z_]+)\s*-*");

            items.Add(new OfficeLicenseItem(
                string.IsNullOrWhiteSpace(name) ? "-" : name,
                string.IsNullOrWhiteSpace(desc) ? "-" : desc,
                DescribeOsppStatus(statusRaw),
                string.IsNullOrWhiteSpace(sku) ? "-" : sku));
        }
        return items;
    }

    public static string DescribeOsppStatus(string? statusRaw) => (statusRaw ?? "").ToUpperInvariant() switch
    {
        "LICENSED" => "Licensed (Đã kích hoạt)",
        "OOB_GRACE" => "Grace period (Chưa kích hoạt - còn hạn dùng thử)",
        "OOT_GRACE" => "Out-of-tolerance grace (Cảnh báo - có thể do thay đổi phần cứng)",
        "NOTIFICATIONS" => "Notification (Chưa kích hoạt - đã hết hạn thông báo)",
        "NON_GENUINE_GRACE" => "Non-genuine grace (Nghi ngờ không hợp lệ)",
        "UNLICENSED" => "Unlicensed (Chưa kích hoạt)",
        "" => "Không xác định",
        var other => $"Không xác định ({other})"
    };

    private static string Match1(string input, string pattern)
    {
        var m = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : "";
    }
}
