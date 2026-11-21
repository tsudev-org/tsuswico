using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Tsudev.Audit.Core.Rendering;

/// <summary>
/// Doc .standards/tokens/design-tokens.json (NHUNG san trong assembly) va sinh ra khoi bien
/// CSS de nhung thang vao the &lt;style&gt; cua bao cao.
///
/// VI SAO PHAI LAM VAY thay vi chep gia tri vao ma nguon:
/// AGENTS.md muc 6 cam hard-code mau / co chu / radius. .standards/tokens/design-tokens.json
/// la nguon chan ly duy nhat cua ca he sinh thai tsudev - sua mot gia tri o do
/// thi bao cao doi theo o lan build sau, khong ai phai di tim trong CSS.
///
/// VI SAO NHUNG vao assembly thay vi doc file: bao cao phai xem duoc khi copy
/// sang may khac, va swico.exe phai chay duoc khi chi copy moi mot file exe.
/// Do cung la ly do Rules/detection-rules.json duoc nhung.
///
/// KHONG duoc de lot mot mau nao ra ngoai file nay. Muc 19 cua bo test quet CSS
/// va bao FAIL neu tim thay ma mau viet cung o HtmlReportRenderer.
/// </summary>
public static class DesignTokens
{
    private const string ResourceName = "Tsudev.Audit.Core.Rendering.design-tokens.json";
    private const string ProductResourceName = "Tsudev.Audit.Core.Rendering.product-swico.json";

    /// <summary>
    /// Bo bo goc theo truong <c>shape</c> cua san pham - sao tu .standards/docs/DESIGN_SYSTEM.md muc 1.2.3
    /// (bo quy uoc khong ghi bang nay dang may doc duoc, chi sinh ra products.css). Test
    /// <c>ProductIdentityTests</c> doi chieu tung gia tri voi .standards/tokens/products.css de bat lech khi nang quy uoc.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string Sm, string Md, string Lg, string Control)> ShapeRadii =
        new Dictionary<string, (string, string, string, string)>(StringComparer.Ordinal)
        {
            ["sac"] = ("2px", "3px", "4px", "3px"),
            ["chuan"] = ("4px", "6px", "8px", "6px"),
            ["mem"] = ("6px", "10px", "14px", "10px"),
            ["tron"] = ("8px", "14px", "20px", "999px"),
        };

    // Nhung khoa la loi giai thich cho nguoi doc, khong phai gia tri.
    private static readonly string[] NotValues = { "usage" };

    private static readonly Lazy<string> _rootCss = new(BuildRootCss, isThreadSafe: true);
    private static readonly Lazy<JsonDocument> _doc = new(() => Load(ResourceName), isThreadSafe: true);
    private static readonly Lazy<JsonDocument> _product = new(() => Load(ProductResourceName), isThreadSafe: true);

    /// <summary>Phien ban bo token dang dung - de ghi vao chan trang bao cao.</summary>
    public static string Version => Str("meta", "version") ?? "?";

    /// <summary>
    /// Khoi <c>:root{...}</c> + bien the toi + bien the in, sinh tu file token.
    /// Ghep vao dau the &lt;style&gt;, TRUOC phan CSS bo cuc.
    /// </summary>
    public static string RootCss => _rootCss.Value;

    /// <summary>Ma <c>id</c> san pham cua lop ban sac (<c>data-product</c>) - "swico".</summary>
    public static string ProductId => Str(_product.Value, "id") ?? "?";

    /// <summary>Bo bo goc cua san pham (sac | chuan | mem | tron).</summary>
    public static string ProductShape => Str(_product.Value, "shape") ?? "chuan";

    /// <summary>Font tieu de cua san pham (h1-h3, so lieu lon); than bai van la font-family chung.</summary>
    public static string ProductFontDisplay => Str(_product.Value, "fontDisplay") ?? "";

    /// <summary>
    /// Mot ma mau theo che do (light | dark) SAU KHI gop de lop ban sac swico len lop nen chung
    /// (DESIGN_SYSTEM muc 7). Vai tro san pham khong khai thi lay lop nen. Nem neu khong co.
    /// </summary>
    public static string Color(string mode, string role)
        => Str(_product.Value, "colors", mode, role) ?? BaseColor(mode, role);

    /// <summary>
    /// Ma mau cua LOP NEN CHUNG (bo vai tro nhan mac dinh cua TsuDEV), KHONG gop ban sac. Chi dung cho khoi
    /// lien ket ve he sinh thai (chu ky "tsudev" o chan trang) - DESIGN_SYSTEM muc 1.2.1: giu nhan dien TsuDEV.
    /// </summary>
    public static string BaseColor(string mode, string role)
    {
        var v = Str(_doc.Value, "color", mode, role);
        return v ?? throw new InvalidOperationException(
            $".standards/tokens/design-tokens.json khong co color.{mode}.{role}");
    }

    private static JsonDocument Load(string resourceName)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Khong tim thay tai nguyen nhung '{resourceName}'. Kiem tra muc EmbeddedResource " +
                "trong Tsudev.Audit.Core.csproj (../../.standards/tokens/...).");
        return JsonDocument.Parse(stream);
    }

    private static string? Str(params string[] path) => Str(_doc.Value, path);

    private static string? Str(JsonDocument doc, params string[] path)
    {
        var el = doc.RootElement;
        foreach (var key in path)
        {
            if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty(key, out el)) return null;
        }
        return el.ValueKind == JsonValueKind.String ? el.GetString() : null;
    }

    private static string BuildRootCss()
    {
        var root = _doc.Value.RootElement;
        var sb = new StringBuilder();

        sb.Append("/* Sinh tu .standards/tokens/design-tokens.json v").Append(Version)
          .Append(" + products/").Append(ProductId).Append(".json")
          .Append(" - KHONG sua tay o day, sua trong file token. */\n:root{\n");

        // --- Mau che do sang (mac dinh)
        AppendColors(sb, root, "light");

        // --- Phan khong doi theo che do
        AppendGroup(sb, root, "typography", "font-family", "--font-sans");
        AppendGroup(sb, root, "typography", "font-mono", "--font-mono");
        AppendMap(sb, root, "--fs-", "typography", "font-size");
        AppendMap(sb, root, "--lh-", "typography", "line-height");
        AppendMap(sb, root, "--fw-", "typography", "weight");
        AppendMap(sb, root, "--ls-", "typography", "letter-spacing");
        AppendRadius(sb, root);
        // Bao cao mo offline, khong nhung tep font: may co font tieu de cua san pham thi dung, khong thi roi ve font than bai.
        sb.Append("  --font-display:")
          .Append(ProductFontDisplay.Length > 0 ? "'" + ProductFontDisplay + "',var(--font-sans)" : "var(--font-sans)")
          .Append(";\n");
        AppendMap(sb, root, "--sp-", "spacing");
        AppendMap(sb, root, "--shadow-", "shadow");
        AppendMap(sb, root, "--z-", "z-index");
        AppendMap(sb, root, "--motion-", "motion");

        // Khoi ma nguon trong bao cao luon dung nen toi, o CA HAI che do - chu
        // trang tren nen toi la cach doc log/khoa registry de nhat. Lay tu bang
        // mau toi cua chinh bo token, khong tu bia mau moi.
        sb.Append("  --c-code-bg:").Append(Color("dark", "bg-base")).Append(";\n");
        sb.Append("  --c-code-ink:").Append(Color("dark", "text-primary")).Append(";\n");

        // Dau trang giu MOT dien mao o ca hai che do, nen luon lay tu bang mau
        // SANG va KHONG bi ghi de trong khoi che do toi.
        //
        // Ly do khong de no lat theo che do nhu phan con lai: o che do toi,
        // primary la #66A3F2 - dau trang se thanh nen SANG. Chu ky thuong hieu
        // duoc chinh cho nen dam se tut xuong 2,4:1, duoi ca nguong 3:1 danh cho
        // chu co lon. Dau trang la mang thuong hieu chu khong phai mot be mat
        // doc noi dung, nen giu co dinh la dung hon la ep no doi mau.
        sb.Append("  --c-hero-from:").Append(Color("light", "primary-active")).Append(";\n");
        sb.Append("  --c-hero-to:").Append(Color("light", "primary")).Append(";\n");
        sb.Append("  --c-hero-ink:").Append(Color("light", "on-primary")).Append(";\n");

        AppendBrand(sb, forDarkScheme: false);
        sb.Append("}\n");

        // --- Che do toi theo he dieu hanh (DESIGN_SYSTEM.md muc 1)
        sb.Append("@media (prefers-color-scheme:dark){\n  :root{\n");
        AppendColors(sb, root, "dark", indent: "    ");
        AppendBrand(sb, forDarkScheme: true, indent: "    ");
        sb.Append("  }\n}\n");

        // --- In ra giay: LUON dung bang mau sang. Khong co dieu nay thi may dat
        //     che do toi se in ra trang giay den kin muc.
        sb.Append("@media print{\n  :root{\n");
        AppendColors(sb, root, "light", indent: "    ");
        AppendBrand(sb, forDarkScheme: false, indent: "    ");
        sb.Append("  }\n}\n");

        return sb.ToString();
    }

    // ---------------------------------------------------------------------
    // MAU CHU KY THUONG HIEU tsudev.
    //
    // CHAN TRANG (nen trang, doi theo che do): theo .standards/docs/BRAND_ASSETS.md,
    // wordmark dung bang chu that MUST lay mau tu token - "tsu" = text-primary,
    // "dev" = text-link - vi hai token nay da qua cong tuong phan o moi che do.
    // Truoc 09/11/2026 o day viet cung #1C5FBF / #D2690A; cam #D2690A chi dat
    // 3,29:1 tren nen bg-base sang (axe bao loi tren bao cao mau). Day cung la loi
    // giai cho QU-6: khong can them gi vao bo token.
    //
    // DAU TRANG (nen gradient mau chinh SANG cua swico, co dinh, khong doi theo che do): chu "Swico" dung
    // vai tro accent cua bang TOI (sac nhat danh cho nen dam) - lay tu token, khong viet cung. Truoc v5.0.0
    // o day viet cung #8FD0FF/#FFA94D cho nen xanh. Do tren goc trai dau trang (primary-active, noi dat chu
    // ky): #FCC181 tren #694003 = 5,6:1.
    //
    // Chan trang "tsudev" la khoi lien ket ve he sinh thai -> giu nhan dien TsuDEV (BaseColor), khong theo swico.

    private static void AppendBrand(StringBuilder sb, bool forDarkScheme, string indent = "  ")
    {
        if (!forDarkScheme)
            sb.Append(indent).Append("--brand-tsu-hero:").Append(Color("dark", "accent")).Append(";\n");

        var mode = forDarkScheme ? "dark" : "light";
        sb.Append(indent).Append("--brand-tsu-foot:").Append(BaseColor(mode, "text-primary")).Append(";\n");
        sb.Append(indent).Append("--brand-dev-foot:").Append(BaseColor(mode, "text-link")).Append(";\n");
    }

    private static void AppendRadius(StringBuilder sb, JsonElement root)
    {
        if (!root.TryGetProperty("radius", out var el)) return;
        if (!ShapeRadii.TryGetValue(ProductShape, out var shape))
            throw new InvalidOperationException($"products/{ProductId}.json: shape '{ProductShape}' khong co trong DESIGN_SYSTEM muc 1.2.3");
        foreach (var p in el.EnumerateObject())
        {
            if (IsNotValue(p)) continue;
            var value = p.Name switch
            {
                "sm" => shape.Sm,
                "md" => shape.Md,
                "lg" => shape.Lg,
                "control" => shape.Control,
                _ => p.Value.GetString(),   // radius-none (khung layout) khong doi theo san pham
            };
            sb.Append("  --radius-").Append(p.Name).Append(':').Append(value).Append(";\n");
        }
    }

    private static void AppendColors(StringBuilder sb, JsonElement root, string mode, string indent = "  ")
    {
        if (!root.TryGetProperty("color", out var colors) || !colors.TryGetProperty(mode, out var set))
            throw new InvalidOperationException($".standards/tokens/design-tokens.json khong co color.{mode}");

        foreach (var p in set.EnumerateObject())
        {
            if (IsNotValue(p)) continue;
            // Gop de lop ban sac swico (DESIGN_SYSTEM muc 7) - vai tro nao san pham khai thi lay ban san pham.
            sb.Append(indent).Append("--c-").Append(p.Name).Append(':').Append(Color(mode, p.Name)).Append(";\n");
        }
    }

    private static void AppendGroup(StringBuilder sb, JsonElement root, string group, string key, string cssName)
    {
        if (root.TryGetProperty(group, out var g) && g.TryGetProperty(key, out var v))
            sb.Append("  ").Append(cssName).Append(':').Append(v.GetString()).Append(";\n");
    }

    private static void AppendMap(StringBuilder sb, JsonElement root, string prefix,
                                  string group, string? sub = null)
    {
        if (!root.TryGetProperty(group, out var el)) return;
        if (sub is not null && !el.TryGetProperty(sub, out el)) return;

        foreach (var p in el.EnumerateObject())
        {
            if (IsNotValue(p)) continue;
            var value = p.Value.ValueKind == JsonValueKind.Number
                ? p.Value.GetRawText()
                : p.Value.GetString();
            sb.Append("  ").Append(prefix).Append(p.Name).Append(':').Append(value).Append(";\n");
        }
    }

    private static bool IsNotValue(JsonProperty p)
        => Array.Exists(NotValues, n => string.Equals(n, p.Name, StringComparison.Ordinal))
           || p.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array;
}
