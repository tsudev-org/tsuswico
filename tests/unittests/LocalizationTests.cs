using System.Globalization;
using System.Text.RegularExpressions;

namespace Tsudev.Audit.Tests;

/// <summary>
/// PRODUCT_POLICY muc 1.4 (phieu T3): lan chay dau theo ngon ngu cua he thong.
/// He thong vi / en / ja ⇒ vi / en / en; lua chon da chon (--lang, TSUSWICO_LANG)
/// luon thang. Giao dien: bao cao HTML theo prefers-color-scheme (sang/toi).
///
/// LUAT: test chay o tieng Anh PHAI dung bao cao MOI (khong dung Infected.Lic/Html
/// - chung la Lazy dung chung, tao ra trong pham vi tieng Anh se lam lech moi
/// test tieng Viet khac).
/// </summary>
public class LocalizationTests
{
    [Theory]
    [InlineData("vi-VN", AppLanguage.Vi)]
    [InlineData("vi", AppLanguage.Vi)]
    [InlineData("en-US", AppLanguage.En)]
    [InlineData("en-GB", AppLanguage.En)]
    [InlineData("ja-JP", AppLanguage.En)]   // ngon ngu chua ho tro ⇒ en
    [InlineData("fr-FR", AppLanguage.En)]
    public void Ngon_ngu_he_thong_quyet_dinh_lan_chay_dau(string culture, AppLanguage expected)
        => Assert.Equal(expected, LanguageResolver.Resolve(null, null, new CultureInfo(culture)));

    [Fact]
    public void Culture_bat_bien_hoac_null_thi_ra_tieng_Anh()
    {
        Assert.Equal(AppLanguage.En, LanguageResolver.FromCulture(CultureInfo.InvariantCulture));
        Assert.Equal(AppLanguage.En, LanguageResolver.FromCulture(null));
    }

    [Fact]
    public void Lua_chon_da_chon_luon_thang_he_thong()
    {
        var ja = new CultureInfo("ja-JP");
        var vi = new CultureInfo("vi-VN");

        Assert.Equal(AppLanguage.Vi, LanguageResolver.Resolve("vi", null, ja));
        Assert.Equal(AppLanguage.En, LanguageResolver.Resolve("en", "vi", vi));       // --lang thang bien moi truong
        Assert.Equal(AppLanguage.Vi, LanguageResolver.Resolve(null, "vi", ja));        // bien moi truong thang he thong
        Assert.Equal(AppLanguage.En, LanguageResolver.Resolve("auto", "en", vi));      // auto = bo qua, xet tiep
        Assert.Equal(AppLanguage.Vi, LanguageResolver.Resolve("auto", null, vi));
    }

    [Fact]
    public void Bien_moi_truong_sai_bi_bo_qua_khong_chan_cong_cu()
        => Assert.Equal(AppLanguage.Vi, LanguageResolver.Resolve(null, "klingon", new CultureInfo("vi-VN")));

    [Fact]
    public void Tim_lang_truoc_khi_phan_tich_tham_so()
    {
        Assert.Equal("en", LanguageResolver.FindCliValue(new[] { "--silent", "--lang", "en" }));
        Assert.Equal("vi", LanguageResolver.FindCliValue(new[] { "--lang=vi", "-s" }));
        Assert.Null(LanguageResolver.FindCliValue(new[] { "--silent" }));
        Assert.Null(LanguageResolver.FindCliValue(new[] { "--lang" }));
    }

    [Fact]
    public void CliOptions_nhan_lang_va_tu_choi_gia_tri_la()
    {
        Assert.Equal(AppLanguage.En, CliOptions.Parse(new[] { "--lang", "EN" }).Language);
        Assert.Equal(AppLanguage.Vi, CliOptions.Parse(new[] { "--lang=vi" }).Language);
        Assert.Null(CliOptions.Parse(new[] { "--lang", "auto" }).Language);
        Assert.Null(CliOptions.Parse(Array.Empty<string>()).Language);
        Assert.Throws<ArgumentException>(() => CliOptions.Parse(new[] { "--lang", "ja" }));
        Assert.Throws<ArgumentException>(() => CliOptions.Parse(new[] { "--lang" }));
    }

    [Fact]
    public void Use_chi_doi_ngon_ngu_trong_pham_vi_roi_tra_lai()
    {
        var before = Loc.Current;
        using (Loc.Use(AppLanguage.En))
        {
            Assert.Equal("en", Loc.T("vi", "en"));
            using (Loc.Use(AppLanguage.Vi)) Assert.Equal("vi", Loc.T("vi", "en"));
            Assert.Equal("en", Loc.T("vi", "en"));
        }
        Assert.Equal(before, Loc.Current);
    }

    /// <summary>Tro giup tieng Anh phai liet ke DUNG bo tham so cua ban tieng Viet.</summary>
    [Fact]
    public void Tro_giup_hai_ngon_ngu_co_cung_bo_tham_so()
    {
        static SortedSet<string> Flags(string text)
            => new(Regex.Matches(text, @"(?<![\w-])--?[a-z][a-z-]*").Select(m => m.Value), StringComparer.Ordinal);

        string vi, en;
        using (Loc.Use(AppLanguage.Vi)) vi = CliOptions.UsageText;
        using (Loc.Use(AppLanguage.En)) en = CliOptions.UsageText;

        Assert.NotEqual(vi, en);
        Assert.Contains("USAGE:", en, StringComparison.Ordinal);
        Assert.Equal(Flags(vi), Flags(en));
        Assert.Contains("--lang", en, StringComparison.Ordinal);
        Assert.Contains("--json-only", en, StringComparison.Ordinal);
    }

    [Fact]
    public void Ma_thoat_co_mo_ta_tieng_Anh_cho_moi_ma()
    {
        using var _ = Loc.Use(AppLanguage.En);
        foreach (var code in new[] { 0, 1, 2, 3, 10, 20, 30, 130 })
            Assert.DoesNotMatch(Vietnamese, ExitCodes.Describe(code));
        Assert.Contains("CRITICAL", ExitCodes.Describe(ExitCodes.VerdictCritical), StringComparison.Ordinal);
    }

    /// <summary>
    /// Bao cao tieng Anh: lang="en", va KHONG con chu tieng Viet nao do chuong
    /// trinh viet ra. Du lieu may gia lap toan ASCII nen bat ky ky tu co dau nao
    /// trong HTML deu la mot chuoi giao dien quen dich.
    /// </summary>
    [Fact]
    public void Bao_cao_tieng_Anh_khong_sot_chuoi_tieng_Viet()
    {
        using var _ = Loc.Use(AppLanguage.En);
        var renderer = new HtmlReportRenderer();
        var lic = LicenseReportBuilder.Build(Infected.NewCtx(), Infected.Options);
        var hw = HardwareReportBuilder.Build(Infected.NewCtx("2026-08-17T10:05:00+07:00"), Infected.Options);

        foreach (var report in new[] { lic, hw })
        {
            var html = renderer.Render(report, "../tsudev-tong-hop.html");
            Assert.Contains("<html lang=\"en\">", html, StringComparison.Ordinal);

            // Bo khoi script: no CO Y chua ca hai bo chuoi (xem test duoi).
            var body = Regex.Replace(html, "<script>.*?</script>", "", RegexOptions.Singleline);
            var leak = Vietnamese.Match(body);
            Assert.False(leak.Success,
                $"con chu tieng Viet trong bao cao en: ...{body.Substring(Math.Max(0, leak.Index - 60), Math.Min(120, body.Length - Math.Max(0, leak.Index - 60)))}...");
            Assert.Contains("Risk score", RenderTextOf(lic), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Bao_cao_tieng_Viet_giu_lang_vi()
    {
        using var _ = Loc.Use(AppLanguage.Vi);
        var html = new HtmlReportRenderer().Render(LicenseReportBuilder.Build(Infected.NewCtx(), Infected.Options));
        Assert.Contains("<html lang=\"vi\">", html, StringComparison.Ordinal);
        Assert.Contains("Điểm rủi ro", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// Khoi script loc/phan trang phai GIONG HET nhau o hai ngon ngu: CSP cua bao
    /// cao mau tren website cho phep script theo BAM. Script doi theo ngon ngu thi
    /// mot trong hai ban se bi chan.
    /// </summary>
    [Fact]
    public void Script_bao_cao_khong_doi_theo_ngon_ngu()
    {
        static string Script(AppLanguage l)
        {
            using var _ = Loc.Use(l);
            var html = new HtmlReportRenderer().Render(LicenseReportBuilder.Build(Infected.NewCtx(), Infected.Options));
            return Regex.Match(html, "<script>(.*?)</script>", RegexOptions.Singleline).Groups[1].Value;
        }

        var vi = Script(AppLanguage.Vi);
        Assert.False(string.IsNullOrEmpty(vi));
        Assert.Equal(vi, Script(AppLanguage.En));
    }

    /// <summary>Giao dien theo he thong: bao cao co ca bang mau sang lan toi (prefers-color-scheme).</summary>
    [Theory]
    [InlineData(AppLanguage.Vi)]
    [InlineData(AppLanguage.En)]
    public void Bao_cao_theo_giao_dien_sang_toi_cua_he_thong(AppLanguage lang)
    {
        using var _ = Loc.Use(lang);
        var html = new HtmlReportRenderer().Render(LicenseReportBuilder.Build(Infected.NewCtx(), Infected.Options));
        Assert.Contains("color-scheme", html, StringComparison.Ordinal);
        Assert.Contains("@media (prefers-color-scheme:dark)", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Trang_tong_hop_tieng_Anh()
    {
        using var _ = Loc.Use(AppLanguage.En);
        var root = Path.Combine(Path.GetTempPath(), "swico-l10n-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = new DashboardBuilder(new HtmlReportRenderer()).BuildAndSave(root);
            var html = File.ReadAllText(path);
            Assert.Contains("<html lang=\"en\">", html, StringComparison.Ordinal);
            Assert.Contains("No reports", html, StringComparison.Ordinal);
            var body = Regex.Replace(html, "<script>.*?</script>", "", RegexOptions.Singleline);
            Assert.DoesNotMatch(Vietnamese, body);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void Phan_loai_phan_mem_theo_ngon_ngu()
    {
        using (Loc.Use(AppLanguage.En)) Assert.Equal("Compression", SoftwareCollector.Classify("7-Zip 22.01"));
        using (Loc.Use(AppLanguage.Vi)) Assert.Equal("Nén/giải nén", SoftwareCollector.Classify("7-Zip 22.01"));
        using (Loc.Use(AppLanguage.En)) Assert.Equal("Uncategorized", SoftwareCollector.Classify("zzz"));
    }

    [Fact]
    public void Json_only_bat_luon_che_do_im_lang()
    {
        var o = CliOptions.Parse(new[] { "--json-only" });
        Assert.True(o.JsonOnly && o.Silent);
        Assert.False(CliOptions.Parse(Array.Empty<string>()).JsonOnly);
    }

    private static string RenderTextOf(AuditReport r) => string.Join("\n",
        r.SummaryCards.Select(c => c.Label).Concat(r.Sections.SelectMany(s => s.Badges.Select(b => b.Text))));

    /// <summary>Ky tu chi tieng Viet moi co (nguyen am co dau + đ).</summary>
    private static readonly Regex Vietnamese = new(
        "[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}
