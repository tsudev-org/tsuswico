using Tsudev.Audit.Core.Localization;
using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>Kiem tra toan ven file he thong/driver qua DISM va SFC.</summary>
public static class SystemIntegrityCollector
{
    public static DataTable Collect(SystemContext ctx, bool runDism, bool runSfc)
    {
        var t = DataTable.Create("", Loc.T("Công cụ kiểm tra", "Check tool"), Loc.T("Kết quả", "Result"), Loc.T("Thời gian chạy", "Run time"));

        // Bao truoc THOI GIAN DU KIEN, khong chi ten cong cu. Day la hai buoc
        // lau nhat trong ca lan quet; khong noi gi thi nguoi dung se tuong may
        // treo va tat cua so - mat luon ket qua da quet duoc.
        if (runDism)
        {
            ctx.Progress.Note(Loc.T("DISM CheckHealth đang chạy (thường dưới 5 giây, tối đa 3 phút)...", "DISM CheckHealth running (usually under 5 seconds, at most 3 minutes)..."));
            var r = ctx.Process.Run("DISM.exe", "/Online /Cleanup-Image /CheckHealth",
                timeoutSeconds: 180, cancellation: ctx.Cancellation);
            t.AddRow("DISM CheckHealth", InterpretDism(r), Loc.T("Nhanh (<5 giây)", "Fast (<5 seconds)"));
        }

        if (runSfc)
        {
            ctx.Progress.Note(Loc.T("sfc /verifyonly đang chạy - việc này mất 5-15 phút, Ctrl+C để dừng...", "sfc /verifyonly running - this takes 5-15 minutes, Ctrl+C to stop..."));
            var r = ctx.Process.Run("sfc.exe", "/verifyonly",
                timeoutSeconds: 1800, cancellation: ctx.Cancellation);
            t.AddRow("System File Checker (sfc /verifyonly)", InterpretSfc(r), Loc.T("Chậm (5-15 phút)", "Slow (5-15 minutes)"));
        }

        t.AddEmptyNotice(Loc.T("Đã tắt trong cấu hình - bật lại bằng tham số --dism / --sfc", "Disabled in configuration - enable with --dism / --sfc"));
        return t;
    }

    public static string InterpretDism(ProcessResult r)
    {
        var text = r.CombinedOutput;
        if (string.IsNullOrWhiteSpace(text))
            return Loc.T("Không chạy được (có thể cần quyền Administrator)", "Could not run (Administrator rights may be needed)");
        if (text.Contains("No component store corruption", StringComparison.OrdinalIgnoreCase))
            return Loc.T("Không phát hiện lỗi (Healthy)", "No errors found (Healthy)");
        if (text.Contains("repairable", StringComparison.OrdinalIgnoreCase))
            return Loc.T("CÓ THỂ SỬA ĐƯỢC - phát hiện lỗi component store, nên chạy DISM /RestoreHealth", "REPAIRABLE - component store errors found, run DISM /RestoreHealth");
        if (text.Contains("corrupt", StringComparison.OrdinalIgnoreCase))
            return Loc.T("PHÁT HIỆN LỖI - nên chạy DISM /RestoreHealth", "ERRORS FOUND - run DISM /RestoreHealth");
        return Loc.T("Không xác định được kết quả", "Result could not be determined");
    }

    public static string InterpretSfc(ProcessResult r)
    {
        var text = r.CombinedOutput;
        if (string.IsNullOrWhiteSpace(text))
            return Loc.T("Không chạy được (có thể cần quyền Administrator)", "Could not run (Administrator rights may be needed)");
        if (text.Contains("did not find any integrity violations", StringComparison.OrdinalIgnoreCase))
            return Loc.T("Không phát hiện file hệ thống bị hỏng/thiếu", "No corrupt/missing system files found");
        if (text.Contains("found corrupt files", StringComparison.OrdinalIgnoreCase))
            return Loc.T("PHÁT HIỆN FILE HỆ THỐNG BỊ HỎNG/THIẾU - nên chạy 'sfc /scannow' với quyền Administrator", "CORRUPT/MISSING SYSTEM FILES FOUND - run 'sfc /scannow' as Administrator");
        return Loc.T("Không xác định được kết quả", "Result could not be determined");
    }
}
