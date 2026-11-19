using Tsudev.Audit.Core.Localization;
using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>Trang thai bao ve va lich su phat hien cua Windows Defender.</summary>
public static class DefenderCollector
{
    public static (DataTable Status, DataTable Threats, int ThreatCount) Collect(SystemContext ctx)
    {
        var status = DataTable.Create(Loc.T("Trạng thái bảo vệ hiện tại", "Current protection status"),
            Loc.T("Bảo vệ thời gian thực", "Real-time protection"), Loc.T("Chống virus bật", "Antivirus enabled"), Loc.T("Phiên bản chữ ký", "Signature version"), Loc.T("Tuổi chữ ký (ngày)", "Signature age (days)"), Loc.T("Quét nhanh gần nhất", "Last quick scan"))
            .Numeric(Loc.T("Tuổi chữ ký (ngày)", "Signature age (days)"));

        var s = ctx.Wmi.Query("MSFT_MpComputerStatus", null, "root\\Microsoft\\Windows\\Defender").FirstRowOrNull();
        if (s is null)
        {
            ctx.Warn(Loc.T("Không đọc được trạng thái Windows Defender (có thể đã bị thay bằng AV khác, hoặc thiếu quyền Administrator).", "Could not read Windows Defender status (it may be replaced by another AV, or Administrator rights are missing)."));
            status.AddRow(Loc.T("Không lấy được", "Unavailable"), "-", "-", "-", "-");
        }
        else
        {
            var quick = WmiDateParser.Parse(s.Str("QuickScanEndTime", ""));
            status.AddRow(
                s.Bool("RealTimeProtectionEnabled") == true ? Loc.T("Đang bật", "On") : Loc.T("ĐANG TẮT - nên bật lại ngay", "OFF - turn it back on now"),
                s.Bool("AntivirusEnabled") == true ? Loc.T("Có", "Yes") : Loc.T("Không", "No"),
                s.Str("AntivirusSignatureVersion"),
                s.Num("AntivirusSignatureAge"),
                quick.HasValue ? DateDisplay.DateTimeText(quick.Value) : Loc.T("Chưa từng quét", "Never scanned"));
        }

        var threats = DataTable.Create(Loc.T("Lịch sử phát hiện mối đe dọa (file .exe, .doc, ... đã bị Defender gắn cờ)", "Threat detection history (.exe, .doc, ... files flagged by Defender)"),
            Loc.T("Tên mối đe dọa", "Threat name"), Loc.T("File/Đường dẫn", "File/Path"), Loc.T("Thời gian phát hiện", "Detected at"), Loc.T("Đã xử lý", "Remediated"));
        threats.Searchable = true;

        var detections = ctx.Wmi.Query("MSFT_MpThreatDetection", null, "root\\Microsoft\\Windows\\Defender");
        int count = 0;
        foreach (var d in detections)
        {
            count++;
            var resources = d.TryGetValue("Resources", out var res) && res is string[] arr && arr.Length > 0
                ? string.Join("; ", arr) : "-";
            threats.AddRow(
                d.Str("ThreatName", d.Str("ThreatID")),
                resources,
                WmiDateParser.Parse(d.Str("InitialDetectionTime", "")) is { } seen ? DateDisplay.DateTimeText(seen) : "-",
                d.Bool("ActionSuccess") == true ? Loc.T("Có", "Yes") : Loc.T("Chưa/Không rõ", "No/Unknown"));
        }
        threats.AddEmptyNotice(Loc.T("Không phát hiện mối đe dọa nào trong lịch sử quét của Windows Defender", "No threats in Windows Defender's scan history"));

        return (status, threats, count);
    }
}
