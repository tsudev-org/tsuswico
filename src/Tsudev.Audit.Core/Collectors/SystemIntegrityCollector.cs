using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>Kiem tra toan ven file he thong/driver qua DISM va SFC.</summary>
public static class SystemIntegrityCollector
{
    public static DataTable Collect(SystemContext ctx, bool runDism, bool runSfc)
    {
        var t = DataTable.Create("", "Công cụ kiểm tra", "Kết quả", "Thời gian chạy");

        // Bao truoc THOI GIAN DU KIEN, khong chi ten cong cu. Day la hai buoc
        // lau nhat trong ca lan quet; khong noi gi thi nguoi dung se tuong may
        // treo va tat cua so - mat luon ket qua da quet duoc.
        if (runDism)
        {
            ctx.Progress.Note("DISM CheckHealth đang chạy (thường dưới 5 giây, tối đa 3 phút)...");
            var r = ctx.Process.Run("DISM.exe", "/Online /Cleanup-Image /CheckHealth",
                timeoutSeconds: 180, cancellation: ctx.Cancellation);
            t.AddRow("DISM CheckHealth", InterpretDism(r), "Nhanh (<5 giây)");
        }

        if (runSfc)
        {
            ctx.Progress.Note("sfc /verifyonly đang chạy - việc này mất 5-15 phút, Ctrl+C để dừng...");
            var r = ctx.Process.Run("sfc.exe", "/verifyonly",
                timeoutSeconds: 1800, cancellation: ctx.Cancellation);
            t.AddRow("System File Checker (sfc /verifyonly)", InterpretSfc(r), "Chậm (5-15 phút)");
        }

        t.AddEmptyNotice("Đã tắt trong cấu hình - bật lại bằng tham số --dism / --sfc");
        return t;
    }

    public static string InterpretDism(ProcessResult r)
    {
        var text = r.CombinedOutput;
        if (string.IsNullOrWhiteSpace(text))
            return "Không chạy được (có thể cần quyền Administrator)";
        if (text.Contains("No component store corruption", StringComparison.OrdinalIgnoreCase))
            return "Không phát hiện lỗi (Healthy)";
        if (text.Contains("repairable", StringComparison.OrdinalIgnoreCase))
            return "CÓ THỂ SỬA ĐƯỢC - phát hiện lỗi component store, nên chạy DISM /RestoreHealth";
        if (text.Contains("corrupt", StringComparison.OrdinalIgnoreCase))
            return "PHÁT HIỆN LỖI - nên chạy DISM /RestoreHealth";
        return "Không xác định được kết quả";
    }

    public static string InterpretSfc(ProcessResult r)
    {
        var text = r.CombinedOutput;
        if (string.IsNullOrWhiteSpace(text))
            return "Không chạy được (có thể cần quyền Administrator)";
        if (text.Contains("did not find any integrity violations", StringComparison.OrdinalIgnoreCase))
            return "Không phát hiện file hệ thống bị hỏng/thiếu";
        if (text.Contains("found corrupt files", StringComparison.OrdinalIgnoreCase))
            return "PHÁT HIỆN FILE HỆ THỐNG BỊ HỎNG/THIẾU - nên chạy 'sfc /scannow' với quyền Administrator";
        return "Không xác định được kết quả";
    }
}
