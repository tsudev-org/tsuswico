using System.Globalization;

namespace Tsudev.Audit.Core.Progress;

/// <summary>
/// Dinh dang dong "da xong" cua tien trinh quet sao cho THANG COT:
///
/// <code>
///     ✓ CPU                                 1.0 s
///     ✓ Tổng quan thiết bị                  1.3 s
/// </code>
///
/// Truoc 20/11/2026 thoi gian dinh sat nhan nen cot thoi gian lech theo do dai
/// nhan (STATE muc 3.5-4b). Nam o Core de test duoc tren moi nen tang.
/// </summary>
public static class ProgressText
{
    /// <summary>
    /// Be rong cot nhan. Du cho nhan dai nhat hien co o ca hai ngon ngu
    /// ("Signs of unauthorized activation" = 32 ky tu); nhan dai hon thi chi
    /// day cot thoi gian sang phai, khong bi cat.
    /// </summary>
    public const int LabelWidth = 34;

    /// <summary>Nhan da dem ve be rong <see cref="LabelWidth"/>.</summary>
    public static string PadLabel(string label)
        => (label ?? "").PadRight(LabelWidth);

    /// <summary>Thoi gian can phai trong 7 ky tu: <c>"  1.3 s"</c>, <c>"612.0 s"</c>.</summary>
    public static string Elapsed(TimeSpan elapsed)
        => string.Create(CultureInfo.InvariantCulture, $"{elapsed.TotalSeconds,5:0.0} s");
}
