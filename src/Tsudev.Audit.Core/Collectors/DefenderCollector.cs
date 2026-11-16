using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>Trang thai bao ve va lich su phat hien cua Windows Defender.</summary>
public static class DefenderCollector
{
    public static (DataTable Status, DataTable Threats, int ThreatCount) Collect(SystemContext ctx)
    {
        var status = DataTable.Create("Trạng thái bảo vệ hiện tại",
            "Bảo vệ thời gian thực", "Chống virus bật", "Phiên bản chữ ký", "Tuổi chữ ký (ngày)", "Quét nhanh gần nhất")
            .Numeric("Tuổi chữ ký (ngày)");

        var s = ctx.Wmi.Query("MSFT_MpComputerStatus", null, "root\\Microsoft\\Windows\\Defender").FirstRowOrNull();
        if (s is null)
        {
            ctx.Warn("Không đọc được trạng thái Windows Defender (có thể đã bị thay bằng AV khác, hoặc thiếu quyền Administrator).");
            status.AddRow("Không lấy được", "-", "-", "-", "-");
        }
        else
        {
            var quick = WmiDateParser.Parse(s.Str("QuickScanEndTime", ""));
            status.AddRow(
                s.Bool("RealTimeProtectionEnabled") == true ? "Đang bật" : "ĐANG TẮT - nên bật lại ngay",
                s.Bool("AntivirusEnabled") == true ? "Có" : "Không",
                s.Str("AntivirusSignatureVersion"),
                s.Num("AntivirusSignatureAge"),
                quick.HasValue ? DateDisplay.DateTimeText(quick.Value) : "Chưa từng quét");
        }

        var threats = DataTable.Create("Lịch sử phát hiện mối đe dọa (file .exe, .doc, ... đã bị Defender gắn cờ)",
            "Tên mối đe dọa", "File/Đường dẫn", "Thời gian phát hiện", "Đã xử lý");
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
                d.Bool("ActionSuccess") == true ? "Có" : "Chưa/Không rõ");
        }
        threats.AddEmptyNotice("Không phát hiện mối đe dọa nào trong lịch sử quét của Windows Defender");

        return (status, threats, count);
    }
}
