using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>Lay output tho cua slmgr.vbs /dli de hien trong khoi &lt;pre&gt;.</summary>
public static class SlmgrCollector
{
    public static string Collect(SystemContext ctx)
    {
        var slmgr = ctx.Files.ExpandEnvironment(@"%SystemRoot%\System32\slmgr.vbs");
        if (!ctx.Files.FileExists(slmgr))
            return "Không tìm thấy slmgr.vbs trên máy này.";

        var result = ctx.Process.Run("cscript.exe", $"//Nologo \"{slmgr}\" /dli",
            timeoutSeconds: 90, cancellation: ctx.Cancellation);
        var text = result.CombinedOutput.Trim();
        return string.IsNullOrWhiteSpace(text)
            ? "Không lấy được kết quả từ slmgr.vbs /dli (có thể cần quyền Administrator)."
            : text;
    }
}
