using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>Chuyen dinh dang ngay WMI (CIM_DATETIME) sang DateTime.</summary>
public static class WmiDateParser
{
    /// <summary>
    /// Dinh dang CIM_DATETIME: yyyyMMddHHmmss.ffffff+UUU
    /// Tra ve null neu chuoi rong/khong hop le (KHONG nem exception).
    /// </summary>
    public static DateTime? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 14) return null;
        var s = value.Trim();
        if (!int.TryParse(s.AsSpan(0, 4), out var year) ||
            !int.TryParse(s.AsSpan(4, 2), out var month) ||
            !int.TryParse(s.AsSpan(6, 2), out var day) ||
            !int.TryParse(s.AsSpan(8, 2), out var hour) ||
            !int.TryParse(s.AsSpan(10, 2), out var min) ||
            !int.TryParse(s.AsSpan(12, 2), out var sec)) return null;
        if (year is < 1601 or > 9999 || month is < 1 or > 12 || day is < 1 or > 31) return null;
        try { return new DateTime(year, month, day, hour, min, sec); }
        catch { return null; }
    }
}
