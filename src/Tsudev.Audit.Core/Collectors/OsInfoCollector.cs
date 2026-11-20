using Tsudev.Audit.Core.Localization;
using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>Thong tin he dieu hanh + may.</summary>
public static class OsInfoCollector
{
    public static DataTable Collect(SystemContext ctx)
    {
        var t = DataTable.Create("", Loc.T("Tên máy", "Computer name"), Loc.T("Hệ điều hành", "Operating system"), Loc.T("Phiên bản (Build)", "Version (Build)"), Loc.T("Kiến trúc", "Architecture"),
            Loc.T("Nhà sản xuất máy", "Manufacturer"), "Model", "BIOS Serial", Loc.T("Ngày cài đặt OS", "OS install date"), Loc.T("Thời gian quét", "Scan time"));

        var os = ctx.Wmi.Query("Win32_OperatingSystem").FirstRowOrNull();
        var cs = ctx.Wmi.Query("Win32_ComputerSystem").FirstRowOrNull();
        var bios = ctx.Wmi.Query("Win32_BIOS").FirstRowOrNull();

        if (os is null) ctx.Warn(Loc.T("Không đọc được Win32_OperatingSystem (thông tin hệ điều hành có thể thiếu).", "Could not read Win32_OperatingSystem (OS information may be incomplete)."));

        string installDate = "-";
        if (os is not null)
        {
            var raw = os.Str("InstallDate", "");
            var parsed = WmiDateParser.Parse(raw);
            if (parsed.HasValue) installDate = DateDisplay.Date(parsed.Value);
        }

        t.AddRow(
            ctx.ComputerName,
            os?.Str("Caption") ?? "-",
            os is null ? "-" : $"{os.Str("Version")} (Build {os.Str("BuildNumber")})",
            os?.Str("OSArchitecture") ?? "-",
            cs?.Str("Manufacturer") ?? "-",
            cs?.Str("Model") ?? "-",
            bios?.Str("SerialNumber") ?? "-",
            installDate,
            DateDisplay.DateTimeText(ctx.ScanTime));

        return t;
    }
}
