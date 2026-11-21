using System.Text.Json;
using System.Text.RegularExpressions;

namespace Tsudev.Audit.Tests;

/// <summary>
/// Ban sac san pham Tsu Swico (tsudev-standards v5.0.0, DESIGN_SYSTEM muc 1.2 + 7; phieu 20261121-STD50):
/// bao cao HTML/.xlsx gop de <c>.standards/tokens/products/swico.json</c> len lop nen chung luc build,
/// KHONG khai lai mau o repo nay. Lop nay canh: gop dung, bo bo goc khop ban sinh products.css,
/// chu ky nha lam giu nhan dien TsuDEV, va khong con vai tro nhan xanh cu cua lop nen trong bao cao.
/// </summary>
public class ProductIdentityTests
{
    private static readonly string Root = RepoRoot();

    private static readonly JsonElement Product =
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, ".standards", "tokens", "products", "swico.json"))).RootElement;

    [Fact]
    public void Lop_ban_sac_la_swico()
    {
        Assert.Equal("swico", DesignTokens.ProductId);
        Assert.Equal(Product.GetProperty("shape").GetString(), DesignTokens.ProductShape);
        Assert.Equal(Product.GetProperty("fontDisplay").GetString(), DesignTokens.ProductFontDisplay);
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void Moi_vai_tro_nhan_lay_tu_tep_san_pham(string mode)
    {
        foreach (var role in Product.GetProperty("colors").GetProperty(mode).EnumerateObject())
        {
            Assert.Equal(role.Value.GetString(), DesignTokens.Color(mode, role.Name));
            // Trong khoi CSS cua bao cao, bien --c-<vai tro> mang gia tri san pham
            Assert.Contains($"--c-{role.Name}:{role.Value.GetString()};", DesignTokens.RootCss, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Vai_tro_nen_khong_bi_san_pham_ghi_de()
        => Assert.Equal(DesignTokens.BaseColor("light", "bg-base"), DesignTokens.Color("light", "bg-base"));

    [Fact]
    public void Bao_cao_khong_con_mau_nhan_xanh_cua_lop_nen()
    {
        var html = Infected.Html;
        foreach (var mode in new[] { "light", "dark" })
            foreach (var role in new[] { "primary", "primary-active", "accent" })
                Assert.DoesNotContain("--c-" + role + ":" + DesignTokens.BaseColor(mode, role) + ";", html, StringComparison.Ordinal);
        // Hai sac chu ky dau trang viet cung truoc v5.0.0
        Assert.DoesNotContain("#8FD0FF", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#FFA94D", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// <c>DesignTokens.ShapeRadii</c> sao bang DESIGN_SYSTEM muc 1.2.3; doi chieu voi ban SINH products.css
    /// de nang bo quy uoc ma bang doi thi test do ngay.
    /// </summary>
    [Fact]
    public void Bo_bo_goc_khop_products_css()
    {
        var css = File.ReadAllText(Path.Combine(Root, ".standards", "tokens", "products.css"));
        var checkedShapes = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(css, @"bo góc (\w+)[^\n]*\n:root\[data-product=""[a-z]+""\] \{(?<body>[^}]*)\}"))
        {
            var shape = m.Groups[1].Value;
            var body = m.Groups["body"].Value;
            Assert.True(DesignTokens.ShapeRadii.TryGetValue(shape, out var r), $"shape '{shape}' co trong ShapeRadii");
            Assert.Contains($"--radius-sm: {r.Sm};", body, StringComparison.Ordinal);
            Assert.Contains($"--radius-md: {r.Md};", body, StringComparison.Ordinal);
            Assert.Contains($"--radius-lg: {r.Lg};", body, StringComparison.Ordinal);
            Assert.Contains($"--radius-control: {r.Control};", body, StringComparison.Ordinal);
            checkedShapes.Add(shape);
        }
        Assert.Contains(DesignTokens.ProductShape, checkedShapes);
        Assert.Contains("--radius-control:" + DesignTokens.ShapeRadii[DesignTokens.ProductShape].Control + ";", DesignTokens.RootCss, StringComparison.Ordinal);
    }

    [Fact]
    public void Font_tieu_de_cua_san_pham_co_duong_roi()
        => Assert.Contains("--font-display:'Space Grotesk',var(--font-sans);", DesignTokens.RootCss, StringComparison.Ordinal);

    [Fact]
    public void Chu_Swico_o_dau_trang_lay_accent_toi_cua_san_pham()
        => Assert.Contains("--brand-tsu-hero:" + DesignTokens.Color("dark", "accent") + ";", DesignTokens.RootCss, StringComparison.Ordinal);

    private static string RepoRoot()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "Tsudev.SystemAudit.sln"))) return d.FullName;
        throw new InvalidOperationException("Khong tim thay goc repo.");
    }
}
