using System.Globalization;

namespace Tsudev.Audit.Core.Localization;

/// <summary>Ngon ngu hien thi cua cong cu va bao cao. Chi hai ngon ngu duoc ho tro.</summary>
public enum AppLanguage { Vi, En }

/// <summary>
/// Chon chuoi hien thi theo ngon ngu dang dung (PRODUCT_POLICY muc 1, phieu T3).
///
/// VI SAO CAP CHUOI NAM CANH NHAU thay vi .resx: moi chuoi tieng Viet va ban
/// tieng Anh cua no nam tren cung mot dong ma, trinh bien dich bat thieu ban
/// dich (khong co khoa nao "quen dich" am tham), va khong can satellite
/// assembly trong ban single-file. Cong cu chi co hai ngon ngu nen bang khoa
/// rieng khong dang chi phi cua no.
///
/// Ngon ngu cua TIEN TRINH dat mot lan luc khoi dong (<see cref="SetProcessLanguage"/>).
/// Test dung <see cref="Use"/> - ghi de theo luong bat dong bo, khong lam
/// lech cac test khac dang chay song song.
/// </summary>
public static class Loc
{
    // Mac dinh Vi: giu nguyen hanh vi cu cho moi noi chua dat ngon ngu (bo test cu).
    private static AppLanguage _process = AppLanguage.Vi;
    private static readonly AsyncLocal<AppLanguage?> Scoped = new();

    public static AppLanguage Current => Scoped.Value ?? _process;

    public static bool IsEnglish => Current == AppLanguage.En;

    public static void SetProcessLanguage(AppLanguage language) => _process = language;

    /// <summary>Doi ngon ngu trong pham vi <c>using</c> (luong bat dong bo hien tai).</summary>
    public static IDisposable Use(AppLanguage language)
    {
        var previous = Scoped.Value;
        Scoped.Value = language;
        return new Restore(previous);
    }

    /// <summary>Chuoi theo ngon ngu dang dung.</summary>
    public static string T(string vi, string en) => IsEnglish ? en : vi;

    /// <summary>Gia tri thuoc tinh <c>lang</c> cua tai lieu HTML.</summary>
    public static string HtmlLang => IsEnglish ? "en" : "vi";

    private sealed class Restore(AppLanguage? previous) : IDisposable
    {
        public void Dispose() => Scoped.Value = previous;
    }
}

/// <summary>
/// Quyet dinh ngon ngu luc khoi dong. Thu tu: <c>--lang</c> &gt; bien moi
/// truong <see cref="EnvVar"/> &gt; ngon ngu giao dien cua he dieu hanh.
/// He dieu hanh tieng Viet ⇒ vi; moi ngon ngu khac (ke ca chua ho tro) ⇒ en.
/// </summary>
public static class LanguageResolver
{
    public const string EnvVar = "TSUSWICO_LANG";

    /// <summary>Cac gia tri hop le cua <c>--lang</c>.</summary>
    public const string Choices = "auto|vi|en";

    public static AppLanguage FromCulture(CultureInfo? culture)
    {
        for (var c = culture; c is not null && !string.IsNullOrEmpty(c.Name); c = c.Parent)
            if (c.TwoLetterISOLanguageName.Equals("vi", StringComparison.OrdinalIgnoreCase))
                return AppLanguage.Vi;
        return AppLanguage.En;
    }

    /// <summary>
    /// Doc mot gia tri chon ngon ngu. Tra ve null cho "auto"/rong (= theo he thong),
    /// nem <see cref="ArgumentException"/> cho gia tri la.
    /// </summary>
    public static AppLanguage? ParseChoice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().ToLowerInvariant() switch
        {
            "auto" or "system" => null,
            "vi" or "vi-vn" => AppLanguage.Vi,
            "en" or "en-us" or "en-gb" => AppLanguage.En,
            _ => throw new ArgumentException(Loc.T(
                $"--lang không hợp lệ: '{value}' (chỉ nhận {Choices}).",
                $"Invalid --lang: '{value}' (accepted: {Choices})."))
        };
    }

    /// <summary>Tim gia tri cua <c>--lang</c> trong tham so, truoc khi phan tich day du.</summary>
    public static string? FindCliValue(IReadOnlyList<string> args)
    {
        for (int i = 0; i < args.Count; i++)
        {
            var a = args[i];
            if (a.Equals("--lang", StringComparison.OrdinalIgnoreCase))
                return i + 1 < args.Count ? args[i + 1] : null;
            if (a.StartsWith("--lang=", StringComparison.OrdinalIgnoreCase))
                return a["--lang=".Length..];
        }
        return null;
    }

    /// <summary>
    /// Ngon ngu cuoi cung. Gia tri sai o bien moi truong bi bo qua (khong chan
    /// cong cu chi vi mot bien moi truong go nham); gia tri sai o <c>--lang</c>
    /// thi <c>CliOptions</c> bao loi tham so nhu moi tham so khac.
    /// </summary>
    public static AppLanguage Resolve(string? cliValue, string? envValue, CultureInfo? uiCulture)
    {
        try
        {
            var cli = ParseChoice(cliValue);
            if (cli is not null) return cli.Value;
        }
        catch (ArgumentException) { /* bao loi o buoc phan tich tham so */ }

        try
        {
            var env = ParseChoice(envValue);
            if (env is not null) return env.Value;
        }
        catch (ArgumentException) { /* bo qua bien moi truong sai */ }

        return FromCulture(uiCulture);
    }
}
