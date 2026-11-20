using Tsudev.Audit.Core.Rendering;

namespace Tsudev.Audit.Tests;

/// <summary>
/// Muc 9c cua bo test cu: thuong hieu trong bao cao HTML. Tu 20/11/2026 bao cao dung CUNG
/// anh voi website swico.tsudev.com (assets/brand/report/, sinh boi brand-assets.mjs):
/// favicon san pham, dau trang = logo san pham, chan trang = logo nha lam tsudev.
/// </summary>
public class BrandingTests
{
    private static string Html => Infected.Html;

    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !File.Exists(Path.Combine(d.FullName, "assets", "brand", "brand.json"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("khong tim thay goc repo");
    }

    private static string DataUriOf(params string[] rel)
        => "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(Path.Combine([RepoRoot, .. rel])));

    [Fact]
    public void Logo_va_favicon_duoc_nhung_thang_vao_trang()
    {
        Assert.True(Html.Contains("data:image/png;base64,", StringComparison.Ordinal),
            "logo duoc NHUNG thang vao trang (khong phu thuoc file ngoai)");
        Assert.True(Html.Contains("<link rel=\"icon\"", StringComparison.Ordinal), "trang co favicon");
        Assert.True(Html.Contains("theme-color", StringComparison.Ordinal), "co mau chu de cho trinh duyet");
        Assert.True(!Html.Contains("src=\"assets/", StringComparison.Ordinal)
                 && !Html.Contains(".png\"", StringComparison.Ordinal),
            "khong tro toi file anh ben ngoai - bao cao copy di van hien duoc logo");
    }

    [Fact]
    public void Favicon_trung_favicon_website()
    {
        var uri = DataUriOf("website", "public", "favicon-32x32.png");
        Assert.Equal(uri, HtmlReportRenderer.FaviconDataUri);
        Assert.Contains("<link rel=\"icon\" type=\"image/png\" sizes=\"32x32\" href=\"" + uri + "\">", Html, StringComparison.Ordinal);
    }

    [Fact]
    public void Anh_nhung_khop_tep_cua_day_chuyen_thuong_hieu()
    {
        Assert.Equal(DataUriOf("assets", "brand", "report", "logo-swico-72.png"), HtmlReportRenderer.ProductLogoDataUri);
        Assert.Equal(DataUriOf("assets", "brand", "report", "logo-tsudev-56.png"), HtmlReportRenderer.MakerLogoDataUri);
    }

    [Fact]
    public void Dau_trang_la_logo_san_pham_lien_ket_trang_san_pham()
    {
        var hero = Html[Html.IndexOf("<header class=\"hero\">", StringComparison.Ordinal)..Html.IndexOf("</header>", StringComparison.Ordinal)];
        Assert.Contains("href=\"https://swico.tsudev.com\"", hero, StringComparison.Ordinal);
        Assert.Contains("<span class=\"brand-logo logo-sp\" aria-hidden=\"true\">", hero, StringComparison.Ordinal);
        Assert.Contains(".logo-sp{background-image:url(\"" + HtmlReportRenderer.ProductLogoDataUri + "\")}", Html, StringComparison.Ordinal);
        Assert.Contains(">Tsu <span class=\"brand-sp\">Swico</span>", hero, StringComparison.Ordinal);
    }

    [Fact]
    public void Chan_trang_giu_khoi_nha_lam_tsudev()
    {
        var foot = Html[Html.IndexOf("<footer class=\"foot\">", StringComparison.Ordinal)..];
        Assert.Contains("href=\"https://tsudev.com\"", foot, StringComparison.Ordinal);
        Assert.Contains("<span class=\"brand-logo logo-tsu\" aria-hidden=\"true\">", foot, StringComparison.Ordinal);
        Assert.Contains(".logo-tsu{background-image:url(\"" + HtmlReportRenderer.MakerLogoDataUri + "\")}", Html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"brand-name\">TsuDEV</span>", foot, StringComparison.Ordinal);
        Assert.True(Html.Contains("rel=\"noopener noreferrer\"", StringComparison.Ordinal),
            "lien ket ra ngoai co rel=noopener noreferrer");
    }

    /// <summary>Moi anh chi nhung MOT lan: favicon + logo san pham + logo nha lam.</summary>
    [Fact]
    public void Anh_nhung_khong_bi_lap()
        => Assert.Equal(3, Html.Split("data:image/png;base64,").Length - 1);

    [Fact]
    public void Thuong_hieu_hien_o_ca_dau_trang_va_chan_trang()
        => Assert.True(Html.Split("class=\"brand").Length - 1 >= 2,
            "thuong hieu hien o ca dau trang va chan trang");
}
