using System.Net;
using System.Text;
using Tsudev.Audit.Core.Localization;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Rendering;

/// <summary>
/// Dung trang HTML tu mot <see cref="AuditReport"/>.
///
/// Nguyen tac bat buoc: MOI chuoi co nguon goc tu du lieu thu thap deu phai di
/// qua <see cref="E"/> truoc khi ghep vao HTML. Ten may, ten phan mem, duong dan
/// file... deu do nguoi khac dat ten - coi chung nhu du lieu KHONG dang tin.
/// Chi CSS/HTML do chinh lop nay sinh ra moi duoc ghep truc tiep.
/// </summary>
public sealed class HtmlReportRenderer
{
    public const string BrandUrl = "https://tsudev.com";
    public const string BrandName = "TsuDEV";

    /// <summary>Ten san pham day du, hien o chan trang va the &lt;meta generator&gt;.</summary>
    public const string ProductName = "Tsu Swico";

    /// <summary>Trang san pham - dau trang bao cao dan ve day, nhu logo o header website.</summary>
    public const string ProductUrl = "https://swico.tsudev.com";

    /// <summary>Mau nen chu dao - theme xanh nhat, dung THONG NHAT ca bao cao lan dashboard.</summary>
    public const string ThemeBackground = "#eaf2fc";

    /// <summary>
    /// Anh thuong hieu NHUNG THANG vao trang duoi dang data URI - CUNG anh voi website
    /// swico.tsudev.com, sinh boi <c>website/scripts/brand-assets.mjs</c> vao
    /// <c>assets/brand/report/</c> (CI <c>--check</c>) va nhung vao assembly (csproj).
    ///
    /// Vi sao khong tro toi file anh ben ngoai: bao cao thuong duoc mo tu file tren dia
    /// (file://), phai xem duoc khi KHONG co mang va copy sang may khac duoc ma khong keo
    /// theo file phu nao. Mot the &lt;img src="logo.png"&gt; se thanh o anh vo ngay khi
    /// ai do copy mot minh file .html di.
    ///
    /// - Favicon: trung byte voi <c>favicon-32x32.png</c> cua website (tab trinh duyet
    ///   cua bao cao giong tab cua trang san pham).
    /// - Logo san pham (dau trang, o 36px, ban 2x): nhu header website.
    /// - Logo nha lam tsudev (chan trang, o 28px, ban 2x): nhu footer website
    ///   (BRAND_ASSETS.md muc 15.5 - khoi "nha lam" giu logo tsudev).
    /// </summary>
    public static readonly string FaviconDataUri = PngDataUri("favicon-32.png");
    public static readonly string ProductLogoDataUri = PngDataUri("logo-swico-72.png");
    public static readonly string MakerLogoDataUri = PngDataUri("logo-tsudev-56.png");

    private static string PngDataUri(string name)
    {
        var res = "Tsudev.Audit.Core.Rendering.brand." + name;
        using var s = typeof(HtmlReportRenderer).Assembly.GetManifestResourceStream(res)
            ?? throw new InvalidOperationException("Thieu tai nguyen nhung " + res + " (Tsudev.Audit.Core.csproj)");
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Ghi chu ve giao dien: bao cao nay CO CHU DICH khong dung
    /// dark-mode tu dong. Ly do: bao cao thuong duoc in ra giay hoac xuat PDF
    /// de luu ho so kiem tra; mot trang tu doi sang nen den theo thiet lap may
    /// nguoi xem se cho ra ban in khong dung y va ton muc.
    /// </summary>
    public string Render(AuditReport report, string? dashboardRelativeLink = null)
    {
        ArgumentNullException.ThrowIfNull(report);

        var sb = new StringBuilder(64 * 1024);
        sb.Append("<!DOCTYPE html>\n<html lang=\"").Append(Loc.HtmlLang).Append("\">\n<head>\n");
        sb.Append("<meta charset=\"utf-8\">\n");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        sb.Append("<link rel=\"icon\" type=\"image/png\" sizes=\"32x32\" href=\"")
          .Append(FaviconDataUri).Append("\">\n");
        // Mau thanh dia chi trinh duyet lay tu token, khong viet cung. Dung ban
        // SANG vi day la mau dau trang o che do mac dinh.
        sb.Append("<meta name=\"theme-color\" content=\"")
          .Append(DesignTokens.Color("light", "primary-active")).Append("\">\n");
        sb.Append("<meta name=\"generator\" content=\"").Append(E(ProductName)).Append("\">\n");
        sb.Append("<title>").Append(E(report.Title)).Append(" - ").Append(E(report.ComputerName)).Append("</title>\n");
        sb.Append("<style>\n").Append(DesignTokens.RootCss).Append(Css).Append(BrandLogoCss)
          .Append("</style>\n</head>\n<body>\n");

        AppendHeader(sb, report, dashboardRelativeLink);
        AppendNav(sb, report);

        sb.Append("<main class=\"wrap\">\n");
        AppendWarnings(sb, report);
        AppendSummaryCards(sb, report);
        foreach (var section in report.Sections) AppendSection(sb, section);
        sb.Append("</main>\n");

        AppendFooter(sb, report);
        sb.Append("</body>\n</html>");
        return sb.ToString();
    }

    // ---------------------------------------------------------------- header

    private static void AppendHeader(StringBuilder sb, AuditReport report, string? dashboardLink)
    {
        sb.Append("<header class=\"hero\">\n<div class=\"wrap\">\n");
        sb.Append("<div class=\"hero-top\">\n");
        AppendProductBrand(sb);
        if (!string.IsNullOrWhiteSpace(dashboardLink))
            sb.Append("<a class=\"back\" href=\"").Append(E(dashboardLink)).Append("\">&larr; ").Append(Loc.T("Trang tổng hợp", "Summary page")).Append("</a>\n");
        sb.Append("</div>\n");

        sb.Append("<h1>").Append(E(report.Title)).Append("</h1>\n");
        sb.Append("<dl class=\"meta\">");
        AppendMeta(sb, Loc.T("Máy", "Computer"), report.ComputerName);
        AppendMeta(sb, Loc.T("Thời điểm quét", "Scan time"),
            DateDisplay.DateTimeText(report.ScanTime));
        AppendMeta(sb, Loc.T("Phiên bản dữ liệu", "Data schema"), report.SchemaVersion);
        sb.Append("</dl>\n</div>\n</header>\n");
    }

    /// <summary>
    /// Dau trang: logo SAN PHAM + ten "Tsu Swico", mot lien ket toi trang san pham -
    /// giong header swico.tsudev.com.
    /// </summary>
    private static void AppendProductBrand(StringBuilder sb)
    {
        AppendBrandLink(sb, "brand", ProductUrl, ProductName, "logo-sp");
        sb.Append("<span class=\"brand-name\">Tsu <span class=\"brand-sp\">Swico</span></span></a>\n");
    }

    /// <summary>
    /// Chan trang: khoi "nha lam" - logo tsudev + "TsuDEV", lien ket toi tsudev.com, giong
    /// footer website (BRAND_ASSETS.md muc 15.5, ECOSYSTEM_IDENTITY.md).
    /// </summary>
    private static void AppendMakerBrand(StringBuilder sb)
    {
        AppendBrandLink(sb, "brand brand-sm", BrandUrl, BrandName, "logo-tsu");
        sb.Append("<span class=\"brand-name\">").Append(E(BrandName)).Append("</span></a>\n");
    }

    private static void AppendBrandLink(StringBuilder sb, string cssClass, string url, string name, string logoClass)
    {
        sb.Append("<a class=\"").Append(cssClass).Append("\" href=\"").Append(url)
          .Append("\" target=\"_blank\" rel=\"noopener noreferrer\" title=\"")
          .Append(E(name)).Append(" - ").Append(url).Append("\">");
        // Anh NEN (khong phai <img>): bao cao khong bao gio co the <img> - test XSS dua vao do
        // (HtmlRenderTests). Ten lien ket do chu ben canh doc len nen logo aria-hidden.
        sb.Append("<span class=\"brand-logo ").Append(logoClass).Append("\" aria-hidden=\"true\"></span>");
    }

    /// <summary>
    /// Anh logo dua vao CSS (MOT lan moi anh, du thuong hieu hien nhieu cho). Tach khoi
    /// <see cref="Css"/> vi data URI doc tu tai nguyen nhung luc chay, khong phai hang so.
    /// </summary>
    private static readonly string BrandLogoCss =
        ".logo-sp{background-image:url(\"" + ProductLogoDataUri + "\")}\n" +
        ".logo-tsu{background-image:url(\"" + MakerLogoDataUri + "\")}\n";

    private static void AppendMeta(StringBuilder sb, string label, string value)
        => sb.Append("<div><dt>").Append(E(label)).Append("</dt><dd>").Append(E(value)).Append("</dd></div>");

    private static void AppendNav(StringBuilder sb, AuditReport report)
    {
        var items = report.Sections.Where(s => !string.IsNullOrWhiteSpace(s.Id)).ToList();
        if (items.Count == 0) return;

        sb.Append("<nav class=\"nav\"><div class=\"wrap nav-inner\">");
        foreach (var s in items)
            sb.Append("<a href=\"#").Append(E(s.Id)).Append("\">")
              .Append(E(string.IsNullOrWhiteSpace(s.NavLabel) ? s.Heading : s.NavLabel)).Append("</a>");
        sb.Append("</div></nav>\n");
    }

    // -------------------------------------------------------------- warnings

    private static void AppendWarnings(StringBuilder sb, AuditReport report)
    {
        if (report.Warnings.Count == 0) return;

        sb.Append("<section class=\"card warn-box\">\n<h2>").Append(Loc.T("Mục thiếu dữ liệu", "Items missing data")).Append("</h2>\n");
        sb.Append("<p class=\"muted\">").Append(Loc.T(
            "Các mục dưới đây không thu thập được. Báo cáo vẫn đầy đủ ở phần còn lại; " +
            "liệt kê ra đây để người đọc biết chỗ nào là khoảng trống chứ không phải kết luận \"sạch\".",
            "The items below could not be collected. The rest of the report is complete; " +
            "they are listed so readers know these are gaps, not a \"clean\" verdict."))
          .Append("</p>\n<ul>\n");
        foreach (var w in report.Warnings) sb.Append("<li>").Append(E(w)).Append("</li>\n");
        sb.Append("</ul>\n</section>\n");
    }

    private static void AppendSummaryCards(StringBuilder sb, AuditReport report)
    {
        if (report.SummaryCards.Count == 0) return;

        sb.Append("<section class=\"cards\">\n");
        foreach (var c in report.SummaryCards)
            sb.Append("<div class=\"stat\"><span class=\"stat-value\">").Append(E(c.Value))
              .Append("</span><span class=\"stat-label\">").Append(E(c.Label)).Append("</span></div>\n");
        sb.Append("</section>\n");
    }

    // -------------------------------------------------------------- sections

    private static void AppendSection(StringBuilder sb, ReportSection section)
    {
        sb.Append("<section class=\"card\" id=\"").Append(E(section.Id)).Append("\">\n");
        sb.Append("<h2>").Append(E(section.Heading)).Append("</h2>\n");

        if (section.Badges.Count > 0)
        {
            sb.Append("<div class=\"badges\">");
            foreach (var b in section.Badges)
                sb.Append("<span class=\"badge ").Append(RiskClass(b.Level)).Append("\">").Append(E(b.Text)).Append("</span>");
            sb.Append("</div>\n");
        }

        if (!string.IsNullOrWhiteSpace(section.Description))
            sb.Append("<p class=\"desc\">").Append(Prose(section.Description)).Append("</p>\n");

        if (section.Verdict is { } v)
        {
            sb.Append("<div class=\"verdict ").Append(VerdictClass(v.Level)).Append("\">");
            sb.Append("<strong>").Append(E(v.Title)).Append("</strong>");
            if (!string.IsNullOrWhiteSpace(v.Detail)) sb.Append("<p>").Append(E(v.Detail)).Append("</p>");
            sb.Append("</div>\n");
        }

        foreach (var t in section.Tables) AppendTable(sb, t);

        if (!string.IsNullOrWhiteSpace(section.PreformattedText))
            sb.Append("<pre class=\"raw\">").Append(E(section.PreformattedText)).Append("</pre>\n");

        if (!string.IsNullOrWhiteSpace(section.MethodNote))
            sb.Append("<details class=\"method\"><summary>").Append(Loc.T("Về phương pháp và giới hạn", "Method and limits")).Append("</summary><p>")
              .Append(Prose(section.MethodNote)).Append("</p></details>\n");

        sb.Append("</section>\n");
    }

    /// <summary>Moc so dong moi trang (.standards/docs/DATA_TABLE.md muc 2): dung nam moc, mac dinh 10.</summary>
    internal static readonly int[] PageSizes = { 10, 20, 50, 100, 200 };

    private static void AppendTable(StringBuilder sb, DataTable table)
    {
        // Bang liet ke (co o loc) duoc phan trang phia trinh duyet: bao cao la tep mot-tep ngoai tuyen,
        // khong co may chu, nen "tai lai tu may chu" cua DATA_TABLE la cat lat tren DOM.
        var paged = table.Searchable && table.Rows.Count > 0;
        sb.Append(paged ? "<div class=\"table-block\" data-paged=\"1\">\n" : "<div class=\"table-block\">\n");

        if (!string.IsNullOrWhiteSpace(table.Title))
            sb.Append("<h3>").Append(E(table.Title)).Append("</h3>\n");
        if (!string.IsNullOrWhiteSpace(table.Description))
            sb.Append("<p class=\"desc\">").Append(Prose(table.Description)).Append("</p>\n");

        // O tim kiem dung JS thuan, khong phu thuoc thu vien ngoai: bao cao phai
        // mo duoc tren may khong co internet.
        if (table.Searchable && table.Rows.Count > 0)
        {
            var filterLabel = Loc.T("Lọc trong bảng", "Filter table");
            sb.Append("<input class=\"filter\" type=\"search\" placeholder=\"").Append(filterLabel).Append("...\" ")
              .Append("oninput=\"tsudevFilter(this)\" aria-label=\"").Append(filterLabel).Append("\">\n");
        }

        // Bang rong thi cuon ngang: vung cuon phai nhan focus de cuon duoc bang ban
        // phim (WCAG 2.1.1; axe scrollable-region-focusable).
        sb.Append("<div class=\"table-scroll\" tabindex=\"0\" role=\"region\" aria-label=\"")
          .Append(E(string.IsNullOrWhiteSpace(table.Title) ? Loc.T("Bảng dữ liệu", "Data table") : table.Title)).Append("\"><table>\n<thead><tr>");
        for (int i = 0; i < table.Columns.Count; i++)
            sb.Append(table.NumericColumns.Contains(i) ? "<th class=\"num\">" : "<th>").Append(E(table.Columns[i])).Append("</th>");
        sb.Append("</tr></thead>\n<tbody>\n");

        foreach (var row in table.Rows)
        {
            sb.Append("<tr>");
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var cell = i < row.Length ? row[i] : "-";
                sb.Append(table.NumericColumns.Contains(i) ? "<td class=\"num\">" : "<td>").Append(E(cell)).Append("</td>");
            }
            sb.Append("</tr>\n");
        }

        sb.Append("</tbody>\n</table></div>\n");

        if (paged)
        {
            // Chan vung ban ghi chia ba (DATA_TABLE muc 3): bo chon trai - tom tat giua - phan trang phai.
            sb.Append("<div class=\"table-foot\"><label class=\"page-size\">").Append(Loc.T("Số dòng mỗi trang", "Rows per page"))
              .Append(" <select data-page-size>");
            foreach (var n in PageSizes)
                sb.Append("<option value=\"").Append(n).Append(n == PageSizes[0] ? "\" selected>" : "\">").Append(n).Append("</option>");
            sb.Append("</select></label><p class=\"table-sum\" aria-live=\"polite\">")
              .Append(table.Rows.Count).Append(Loc.T(" dòng", " rows")).Append("</p><nav class=\"pager\" aria-label=\"")
              .Append(Loc.T("Phân trang", "Pagination")).Append("\"></nav></div>\n");
        }

        sb.Append("</div>\n");
    }

    private static void AppendFooter(StringBuilder sb, AuditReport report)
    {
        sb.Append("<footer class=\"foot\"><div class=\"wrap\">\n");
        sb.Append("<div class=\"foot-brand\">");
        AppendMakerBrand(sb);
        sb.Append("<p>").Append(Loc.T("Báo cáo sinh tự động bởi", "Report generated by")).Append(" <a href=\"").Append(ProductUrl)
          .Append("\" target=\"_blank\" rel=\"noopener noreferrer\">").Append(E(ProductName))
          .Append("</a> &middot; ").Append(Loc.T("lược đồ dữ liệu", "data schema")).Append(' ')
          .Append(E(report.SchemaVersion)).Append("</p>");
        sb.Append("</div>\n");
        sb.Append("<p class=\"muted\">").Append(Loc.T(
            "Số liệu phản ánh trạng thái máy tại thời điểm quét. " +
            "Đây là dữ liệu kỹ thuật để tham khảo, không phải kết luận pháp lý.",
            "Figures reflect the machine's state at scan time. " +
            "This is technical data for reference, not a legal conclusion.")).Append("</p>\n");
        sb.Append("</div></footer>\n");
        sb.Append("<script>\n").Append(FilterScript).Append("</script>\n");
    }

    // ----------------------------------------------------------------- utils

    /// <summary>
    /// Escape HTML. Day la HANG RAO DUY NHAT chong HTML injection - moi du lieu
    /// thu thap deu phai di qua day. Co unit test bao ve hanh vi nay.
    /// </summary>
    internal static string E(string? value) => WebUtility.HtmlEncode(value ?? "");

    /// <summary>
    /// Van ban mo ta (Description, MethodNote) duoc phep mang DUNG NAM the trinh
    /// bay: b, /b, code, /code, br. Cach lam: ma hoa TOAN BO truoc, roi chi mo lai
    /// dung nam chuoi da ma hoa do. The co thuoc tinh (vi du "b onclick=...") khong
    /// khop chuoi nao nen van bi ma hoa - du lieu doc tu may duoc quet (ten SKU,
    /// ten phan mem) di qua day cung khong the chen script hay thuoc tinh.
    ///
    /// Truoc 09/11/2026 cac mo ta di qua E() nen bao cao in nguyen van
    /// "&lt;b&gt;hop le&lt;/b&gt;" ra man hinh.
    /// </summary>
    internal static string Prose(string? value)
    {
        var s = E(value);
        foreach (var tag in ProseTags)
            s = s.Replace(WebUtility.HtmlEncode(tag), tag, StringComparison.Ordinal);
        return s;
    }

    private static readonly string[] ProseTags = { "<b>", "</b>", "<code>", "</code>", "<br>" };

    internal static string RiskClass(RiskLevel level) => level switch
    {
        RiskLevel.Critical => "lv-critical",
        RiskLevel.High => "lv-high",
        RiskLevel.Medium => "lv-medium",
        RiskLevel.Low => "lv-low",
        _ => "lv-none"
    };

    internal static string VerdictClass(VerdictLevel level) => level switch
    {
        VerdictLevel.Ok => "vd-ok",
        VerdictLevel.Warning => "vd-warn",
        VerdictLevel.Bad => "vd-bad",
        _ => "vd-unknown"
    };

    // Loc + phan trang (DATA_TABLE): JS thuan, khong thu vien ngoai. Su kien cua bo chon / nut trang gan TRONG
    // script (khong them on*= noi tuyen) - CSP cua bao cao mau tren website chi cho bam cua dung khoi script nay
    // va cua chuoi "tsudevFilter(this)". Doi khoi nay => cap nhat bam trong website/public/_headers.
    private const string FilterScript = """
        (function () {
          var SIZES = [10, 20, 50, 100, 200];
          var EN = document.documentElement.lang === 'en';
          var T = EN ? { none: 'No matching rows', from: ' (filtered from ', prev: 'Previous page', next: 'Next page', page: 'Page ' }
                     : { none: 'Không có dòng nào khớp', from: ' (lọc từ ', prev: 'Trang trước', next: 'Trang sau', page: 'Trang ' };
          function key(b) { var h = b.querySelector('h3'); return 'swico-page-size:' + (h ? h.textContent : ''); }
          function readSize(b) {
            try { var v = parseInt(localStorage.getItem(key(b)), 10); if (SIZES.indexOf(v) >= 0) return v; } catch (e) { }
            return 10;
          }
          function render(b) {
            var st = b.tsudev, rows = b.querySelectorAll('tbody tr'), match = [], i;
            for (i = 0; i < rows.length; i++) {
              if (rows[i].textContent.toLowerCase().indexOf(st.q) === -1) rows[i].style.display = 'none'; else match.push(rows[i]);
            }
            var pages = Math.max(1, Math.ceil(match.length / st.size));
            if (st.page >= pages) st.page = pages - 1;
            var from = st.page * st.size, to = Math.min(match.length, from + st.size);
            for (i = 0; i < match.length; i++) match[i].style.display = i >= from && i < to ? '' : 'none';
            var sum = b.querySelector('.table-sum');
            sum.textContent = match.length === 0 ? T.none :
              (from + 1) + '-' + to + ' / ' + match.length + (match.length < rows.length ? T.from + rows.length + ')' : '');
            var nav = b.querySelector('.pager'); nav.textContent = '';
            if (pages < 2) return;
            function btn(label, p, cur, aria) {
              var x = document.createElement('button'); x.type = 'button'; x.textContent = label;
              if (aria) x.setAttribute('aria-label', aria);
              if (cur) x.setAttribute('aria-current', 'page');
              if (p < 0 || p >= pages) x.disabled = true;
              x.addEventListener('click', function () { st.page = p; render(b); });
              nav.appendChild(x);
            }
            btn('<', st.page - 1, false, T.prev);
            var shown = {}, list = [0, st.page - 1, st.page, st.page + 1, pages - 1], last = -1;
            list.sort(function (a, c) { return a - c; });
            for (i = 0; i < list.length; i++) {
              var p = list[i]; if (p < 0 || p >= pages || shown[p]) continue; shown[p] = 1;
              if (last >= 0 && p > last + 1) { var g = document.createElement('span'); g.textContent = '...'; nav.appendChild(g); }
              btn(String(p + 1), p, p === st.page, T.page + (p + 1)); last = p;
            }
            btn('>', st.page + 1, false, T.next);
          }
          function init() {
            var blocks = document.querySelectorAll('.table-block[data-paged]');
            for (var i = 0; i < blocks.length; i++) (function (b) {
              b.tsudev = { q: '', page: 0, size: readSize(b) };
              var sel = b.querySelector('select[data-page-size]');
              sel.value = String(b.tsudev.size);
              sel.addEventListener('change', function () {
                var first = b.tsudev.page * b.tsudev.size, v = parseInt(sel.value, 10);
                if (SIZES.indexOf(v) < 0) v = 10;
                b.tsudev.size = v; b.tsudev.page = Math.floor(first / v);
                try { localStorage.setItem(key(b), String(v)); } catch (e) { }
                render(b);
              });
              render(b);
            })(blocks[i]);
          }
          window.tsudevFilter = function (input) {
            var b = input.closest('.table-block');
            if (!b) return;
            if (!b.tsudev) {
              var rows = b.querySelectorAll('tbody tr'), q = input.value.toLowerCase();
              for (var i = 0; i < rows.length; i++) rows[i].style.display = rows[i].textContent.toLowerCase().indexOf(q) === -1 ? 'none' : '';
              return;
            }
            b.tsudev.q = input.value.toLowerCase(); b.tsudev.page = 0; render(b);
          };
          if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init); else init();
        })();
        """;

    /// <summary>
    /// CSS nhung thang vao trang: bao cao phai xem duoc khi khong co mang va khi
    /// duoc copy sang may khac ma khong keo theo file phu nao.
    /// </summary>
    private const string Css = $$"""
        *{box-sizing:border-box}
        body{margin:0;background:var(--c-bg-base);color:var(--c-text-primary);
             font-family:var(--font-sans);font-size:var(--fs-body-web);line-height:var(--lh-body)}
        .wrap{max-width:1180px;margin:0 auto;padding:0 var(--sp-5)}
        a{color:var(--c-text-link)}
        h1,h2,h3,.brand-name{font-family:var(--font-display)}
        .muted{color:var(--c-text-muted)}
        :focus-visible{outline:2px solid var(--c-focus-ring);outline-offset:2px}

        /* Dau trang GIU nguyen dien mao o ca hai che do - xem DesignTokens. */
        .hero{background:linear-gradient(160deg,var(--c-hero-from),var(--c-hero-to));
              color:var(--c-hero-ink);padding:var(--sp-6) 0}
        .hero-top{display:flex;justify-content:space-between;align-items:center;
                  gap:var(--sp-4);margin-bottom:var(--sp-4)}

        /* Thuong hieu - giong website: dau trang = logo san pham + "Tsu Swico" (lien ket
           swico.tsudev.com); chan trang = logo tsudev + "TsuDEV" (lien ket tsudev.com). */
        .brand{display:inline-flex;align-items:center;gap:var(--sp-3);text-decoration:none;line-height:1}
        .brand-logo{display:block;width:36px;height:36px;flex:none;background:no-repeat center/contain}
        .brand-name{font-size:var(--fs-h3);font-weight:var(--fw-bold);letter-spacing:var(--ls-heading)}
        /* Dau trang nen gradient xanh CO DINH (khong doi theo che do): chu sang, "Swico"
           xanh nhat nhu chu nhan manh o header website. Xem DesignTokens. */
        .hero .brand{color:var(--c-hero-ink)}
        .hero .brand-sp{color:var(--brand-tsu-hero)}
        .brand:hover .brand-name{filter:brightness(1.12)}

        .brand-sm .brand-logo{width:28px;height:28px}
        .brand-sm .brand-name{font-size:var(--fs-h4)}
        .foot .brand{color:var(--brand-tsu-foot)}
        .foot-brand{display:flex;align-items:center;gap:var(--sp-4);flex-wrap:wrap}
        .foot-brand p{margin:0}
        .back{color:var(--c-hero-ink);opacity:.85;text-decoration:none;font-size:var(--fs-body-desktop)}
        .back:hover{text-decoration:underline;opacity:1}
        .hero h1{margin:0 0 var(--sp-3);font-size:var(--fs-h1);line-height:var(--lh-heading);
                 letter-spacing:var(--ls-heading)}
        .meta{display:flex;flex-wrap:wrap;gap:var(--sp-3) var(--sp-8);margin:0}
        /* KHONG viet hoa toan bo: nhan o day dai hon 2 tu ("Thoi diem quet") va
           tieng Viet co dau viet hoa het thi kho doc - DESIGN_SYSTEM.md muc 4. */
        .meta dt{font-size:var(--fs-xs);font-weight:var(--fw-medium);
                 color:var(--c-hero-ink);opacity:.78;margin:0}
        .meta dd{margin:0;font-weight:var(--fw-semibold)}

        .nav{position:sticky;top:0;z-index:var(--z-sticky);background:var(--c-bg-surface);
             border-bottom:1px solid var(--c-border);box-shadow:var(--shadow-sm)}
        .nav-inner{display:flex;flex-wrap:wrap;gap:var(--sp-1);overflow-x:auto}
        .nav a{padding:var(--sp-3);text-decoration:none;font-size:var(--fs-body-desktop);
               white-space:nowrap;border-bottom:2px solid transparent}
        .nav a:hover{border-bottom-color:var(--c-primary);background:var(--c-bg-hover)}

        .cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));
               gap:var(--sp-4);margin:var(--sp-6) 0}
        .stat{background:var(--c-bg-surface);border:1px solid var(--c-border);
              border-radius:var(--radius-lg);padding:var(--sp-4);
              display:flex;flex-direction:column;gap:var(--sp-1)}
        .stat-value{font-size:var(--fs-h2);font-weight:var(--fw-bold);line-height:var(--lh-heading)}
        .stat-label{font-size:var(--fs-sm);color:var(--c-text-muted)}

        .card{background:var(--c-bg-surface);border:1px solid var(--c-border);
              border-radius:var(--radius-lg);padding:var(--sp-6);margin:var(--sp-5) 0;
              box-shadow:var(--shadow-sm)}
        .card h2{margin:0 0 var(--sp-4);font-size:var(--fs-h3);padding-bottom:var(--sp-3);
                 line-height:var(--lh-heading);border-bottom:1px solid var(--c-border)}
        .card h3{margin:var(--sp-5) 0 var(--sp-2);font-size:var(--fs-body-web-lg);
                 line-height:var(--lh-heading);color:var(--c-primary-active)}
        /* 72ch: gioi han chieu rong khoi van ban dai de mat khong moi khi dao dong. */
        .desc{margin:0 0 var(--sp-3);max-width:72ch;color:var(--c-text-secondary);
              font-size:var(--fs-body-desktop)}

        .warn-box{border-left:5px solid var(--c-warning)}
        .warn-box ul{margin:0;padding-left:var(--sp-5)}
        .warn-box li{margin:var(--sp-1) 0}

        .badges{display:flex;flex-wrap:wrap;gap:var(--sp-2);margin-bottom:var(--sp-3)}
        /* Nen SURFACE chu khong phai bg-subtle: mau danger tren nen subtle chi dat
           4,3:1, duoi nguong AA 4,5:1 ma DESIGN_SYSTEM.md muc 1 bat buoc. */
        .badge{display:inline-block;padding:var(--sp-1) var(--sp-3);border-radius:var(--radius-sm);
               font-size:var(--fs-xs);font-weight:var(--fw-semibold);
               background:var(--c-bg-surface);border:1px solid var(--c-border)}
        .lv-none{color:var(--c-text-muted)}
        .lv-low{color:var(--c-success)}
        .lv-medium{color:var(--c-warning)}
        .lv-high{color:var(--c-danger)}
        .lv-critical{background:var(--c-danger);color:var(--c-on-status);border-color:var(--c-danger)}

        .verdict{border-radius:var(--radius-lg);padding:var(--sp-4);margin:0 0 var(--sp-4);
                 background:var(--c-bg-subtle);border-left:5px solid}
        .verdict p{margin:var(--sp-2) 0 0;font-size:var(--fs-body-desktop)}
        .vd-ok{border-color:var(--c-success)}
        .vd-warn{border-color:var(--c-warning)}
        .vd-bad{border-color:var(--c-danger)}
        .vd-unknown{border-color:var(--c-border-strong)}

        .filter{width:100%;max-width:340px;margin:0 0 var(--sp-3);
                padding:var(--sp-2) var(--sp-3);font-family:inherit;font-size:var(--fs-body-desktop);
                border:1px solid var(--c-border);border-radius:var(--radius-md);
                background:var(--c-bg-surface);color:inherit}
        .table-scroll{overflow-x:auto;border:1px solid var(--c-border);border-radius:var(--radius-lg)}
        table{width:100%;border-collapse:collapse;font-size:var(--fs-body-desktop)}
        th,td{padding:var(--sp-2) var(--sp-4);text-align:left;
              border-bottom:1px solid var(--c-border);vertical-align:top}
        th{background:var(--c-bg-subtle);font-weight:var(--fw-semibold);
           white-space:nowrap;position:sticky;top:0}
        th.num,td.num{text-align:right;font-variant-numeric:tabular-nums}
        tbody tr:nth-child(even){background:var(--c-bg-base)}
        tbody tr:hover{background:var(--c-bg-hover)}

        /* Chan vung ban ghi (DATA_TABLE muc 3): bo chon trai - tom tat giua - phan trang phai */
        .table-foot{display:flex;flex-wrap:wrap;align-items:center;gap:var(--sp-2) var(--sp-4);
                    margin-top:var(--sp-2);font-size:var(--fs-sm);color:var(--c-text-muted)}
        .page-size select{margin-left:var(--sp-2);height:32px;font:inherit;color:var(--c-text-primary);
                          background:var(--c-bg-surface);border:1px solid var(--c-border-control);border-radius:var(--radius-md)}
        .table-sum{flex:1;margin:0;text-align:center}
        .pager{display:flex;gap:var(--sp-1);align-items:center}
        .pager button{min-width:32px;height:32px;padding:0 var(--sp-2);font:inherit;cursor:pointer;
                      color:var(--c-text-primary);background:var(--c-bg-surface);
                      border:1px solid var(--c-border-control);border-radius:var(--radius-md)}
        .pager button[aria-current="page"]{background:var(--c-primary);border-color:var(--c-primary);color:var(--c-on-primary)}
        .pager button:disabled{opacity:.45;cursor:not-allowed}

        /* Khoi du lieu tho luon dung nen toi o CA HAI che do - chu sang tren nen
           toi la cach doc log va khoa registry de nhat. Mau lay tu bang mau toi
           cua chinh bo token, khong bia them mau moi. */
        pre.raw{background:var(--c-code-bg);color:var(--c-code-ink);padding:var(--sp-4);
                border-radius:var(--radius-md);overflow-x:auto;
                font-family:var(--font-mono);font-size:var(--fs-sm);line-height:var(--lh-body);
                white-space:pre-wrap;word-break:break-word}
        details.method{margin-top:var(--sp-4);font-size:var(--fs-body-desktop);
                       color:var(--c-text-secondary)}
        details.method summary{cursor:pointer;font-weight:var(--fw-semibold);color:var(--c-primary-active)}

        .foot{border-top:1px solid var(--c-border);margin-top:var(--sp-8);
              padding:var(--sp-5) 0 var(--sp-10);font-size:var(--fs-sm)}
        .foot p{margin:var(--sp-1) 0}

        @media print{
          .nav,.filter,.table-foot{display:none}
          /* In ra giay: in DU moi dong, khong theo trang dang xem */
          .table-block[data-paged] tbody tr{display:table-row !important}
          /* In ra giay: giu logo (anh nen) de ban in van co nhan dien */
          .brand-logo{-webkit-print-color-adjust:exact;print-color-adjust:exact}
          /* Bang mau in da bi ep ve che do SANG o khoi bien do DesignTokens sinh,
             nen khong can ep mau o day nua - chi bo nen mau cua trang. */
          body{background:var(--c-bg-surface)}
          .card{break-inside:avoid;box-shadow:none}
          .hero{background:var(--c-hero-from) !important;
                -webkit-print-color-adjust:exact;print-color-adjust:exact}
        }
        """;
}
