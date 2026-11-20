using Tsudev.Audit.Core.Localization;
using System.Globalization;
using System.Text;
using Tsudev.Audit.Core.Serialization;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Rendering;

/// <summary>
/// Dung trang tong hop <c>tsudev-tong-hop.html</c> tu moi file JSON tim thay
/// ben duoi thu muc cap 2.
///
/// Cach dung thuc te: ky thuat vien quet nhieu may, roi copy cac thu muc ket qua
/// cap 3 tu may khac vao cung mot thu muc cap 2. Vi vay viec quet phai DE QUY
/// va KHONG GIOI HAN DO SAU - khong the doan truoc nguoi dung se long thu muc
/// sau bao nhieu tang khi copy qua USB hay chia se mang.
/// </summary>
public sealed class DashboardBuilder
{
    public const string OutputFileName = "tsudev-tong-hop.html";

    private readonly HtmlReportRenderer _renderer;

    public DashboardBuilder(HtmlReportRenderer renderer)
        => _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

    /// <summary>
    /// Quet <paramref name="level2Directory"/>, dung trang tong hop va ghi ra dia.
    /// Tra ve duong dan day du cua file da ghi.
    /// </summary>
    public string BuildAndSave(string level2Directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(level2Directory);

        var entries = Scan(level2Directory);
        var html = Build(entries);
        var outputPath = Path.Combine(level2Directory, OutputFileName);

        Directory.CreateDirectory(level2Directory);
        File.WriteAllText(outputPath, html, new UTF8Encoding(false));
        return outputPath;
    }

    /// <summary>
    /// Doc moi file .json ben duoi thu muc goc. File hong hoac khong phai bao
    /// cao deu bi BO QUA LANG LE - mot file rac trong thu muc khong duoc lam
    /// hong ca trang tong hop.
    /// </summary>
    public IReadOnlyList<DashboardEntry> Scan(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        if (!Directory.Exists(rootDirectory)) return Array.Empty<DashboardEntry>();

        var results = new List<DashboardEntry>();

        IEnumerable<string> files;
        try
        {
            // EnumerationOptions voi RecurseSubdirectories: khong dat MaxRecursionDepth
            // -> khong gioi han do sau, dung nhu yeu cau. IgnoreInaccessible de mot
            // thu muc bi chan quyen khong lam dung ca qua trinh quet.
            files = Directory.EnumerateFiles(rootDirectory, "*.json", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            });
        }
        catch (IOException) { return Array.Empty<DashboardEntry>(); }
        catch (UnauthorizedAccessException) { return Array.Empty<DashboardEntry>(); }

        foreach (var file in files)
        {
            AuditReport? report;
            try
            {
                report = AuditJson.ReadReport(File.ReadAllText(file));
            }
            catch (System.Text.Json.JsonException) { continue; }
            catch (IOException) { continue; }
            catch (UnauthorizedAccessException) { continue; }

            // Kiem tra toi thieu de chac chan day la bao cao cua chinh cong cu nay,
            // khong phai mot file .json bat ky nam cung thu muc.
            if (report is null || string.IsNullOrWhiteSpace(report.ComputerName)
                || string.IsNullOrWhiteSpace(report.SchemaVersion)) continue;

            var htmlPath = ResolveHtmlLink(file, report, rootDirectory);
            results.Add(new DashboardEntry(report, htmlPath));
        }

        // Sap xep: may theo van A-Z, trong cung mot may thi ban quet moi nhat len truoc.
        return results
            .OrderBy(e => e.Report.ComputerName, StringComparer.CurrentCultureIgnoreCase)
            .ThenByDescending(e => e.Report.ScanTime)
            .ToList();
    }

    /// <summary>
    /// Duong dan tuong doi (dung dau '/') tu thu muc cap 2 toi file HTML di kem
    /// bao cao. Neu file HTML khong ton tai thi tra ve null - hien text thuong
    /// thay vi mot lien ket gay loi 404 khi bam vao.
    /// </summary>
    private static string? ResolveHtmlLink(string jsonPath, AuditReport report, string rootDirectory)
    {
        var folder = Path.GetDirectoryName(jsonPath);
        if (folder is null) return null;

        var htmlName = string.IsNullOrWhiteSpace(report.HtmlReportFile)
            ? Path.ChangeExtension(Path.GetFileName(jsonPath), ".html")
            : report.HtmlReportFile;

        var full = Path.Combine(folder, htmlName);
        if (!File.Exists(full)) return null;

        return Path.GetRelativePath(rootDirectory, full).Replace(Path.DirectorySeparatorChar, '/');
    }

    // ----------------------------------------------------------------- dung

    public string Build(IReadOnlyList<DashboardEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var machines = entries.Select(e => e.Report.ComputerName)
            .Distinct(StringComparer.CurrentCultureIgnoreCase).Count();
        var suspicious = entries.Count(e => e.Report.VerdictLevel is VerdictLevel.Bad or VerdictLevel.Warning);
        var latestScan = entries.Count > 0 ? entries.Max(e => e.Report.ScanTime) : (DateTimeOffset?)null;

        var report = new AuditReport
        {
            ReportKind = ReportKind.LicenseAudit,
            Title = Loc.T("Tổng hợp kết quả rà quét", "Scan results summary"),
            ComputerName = machines == 1 ? entries[0].Report.ComputerName : Loc.T($"{machines} máy", $"{machines} computers"),
            ScanTime = latestScan ?? DateTimeOffset.Now,
            SummaryCards =
            {
                new SummaryCard { Value = machines.ToString(CultureInfo.InvariantCulture), Label = Loc.T("Máy đã quét", "Computers scanned") },
                new SummaryCard { Value = entries.Count.ToString(CultureInfo.InvariantCulture), Label = Loc.T("Bản báo cáo", "Reports") },
                new SummaryCard { Value = suspicious.ToString(CultureInfo.InvariantCulture), Label = Loc.T("Báo cáo cần xem lại", "Reports to review") }
            }
        };

        var section = new ReportSection
        {
            Id = "sec-tong-hop",
            NavLabel = Loc.T("Tổng hợp", "Summary"),
            Heading = Loc.T("Toàn bộ báo cáo tìm thấy", "All reports found"),
            Description = entries.Count == 0
                ? Loc.T("Chưa tìm thấy báo cáo nào trong thư mục này. Chạy công cụ, hoặc copy thư mục kết quả từ máy khác vào đây rồi mở lại trang này.", "No reports found in this folder yet. Run the tool, or copy result folders from other computers here and reopen this page.")
                : Loc.T("Bảng gom mọi báo cáo nằm trong thư mục này và mọi thư mục con, không giới hạn độ sâu. Copy thư mục kết quả từ máy khác vào đây rồi chạy lại công cụ là bảng tự cập nhật.", "This table gathers every report in this folder and all subfolders, at any depth. Copy result folders from other computers here and run the tool again to refresh it."),
            MethodNote = Loc.T("Trang này dựng lại từ các file .json đi kèm mỗi báo cáo. File .json hỏng hoặc không đúng định dạng sẽ bị bỏ qua, nên số dòng ở đây có thể ít hơn số thư mục bạn nhìn thấy.", "This page is rebuilt from the .json file next to each report. Broken or malformed .json files are skipped, so there may be fewer rows here than folders you see.")
        };

        var table = DataTable.Create(Loc.T("Danh sách báo cáo", "Reports"),
            Loc.T("Máy", "Computer"), Loc.T("Loại báo cáo", "Report type"), Loc.T("Thời điểm quét", "Scan time"), Loc.T("Kết luận", "Verdict"), Loc.T("Điểm rủi ro", "Risk score"), Loc.T("Số phát hiện", "Findings"), Loc.T("Cấu hình", "Configuration"), Loc.T("Mở báo cáo", "Open report"))
            .Numeric(Loc.T("Điểm rủi ro", "Risk score"), Loc.T("Số phát hiện", "Findings"));
        table.Searchable = true;

        foreach (var e in entries)
        {
            var r = e.Report;
            table.AddRow(
                r.ComputerName,
                r.ReportKind == ReportKind.LicenseAudit ? Loc.T("Bản quyền", "License") : Loc.T("Phần cứng", "Hardware"),
                DateDisplay.DateTimeText(r.ScanTime),
                r.VerdictText ?? VerdictLabel(r.VerdictLevel),
                r.RiskScore is null ? "-" : $"{r.RiskScore.Value}/100 ({r.RiskScore.Label})",
                r.RiskFindingsCount.ToString(CultureInfo.InvariantCulture),
                r.HardwareSummary ?? "-",
                e.HtmlRelativePath ?? Loc.T("(không tìm thấy file HTML)", "(HTML file not found)"));
        }

        if (entries.Count == 0) table.AddEmptyNotice(Loc.T("Chưa có báo cáo nào.", "No reports yet."));
        section.Tables.Add(table);
        report.Sections.Add(section);

        var html = _renderer.Render(report);

        // Bien cot cuoi thanh lien ket bam duoc. Lam sau khi render vi
        // HtmlReportRenderer co chu dich escape MOI thu - do la hang rao chong
        // HTML injection va khong duoc noi long. O day ta thay the chuoi DA
        // ESCAPE cua duong dan bang the <a> co href cung do ta tu dung.
        foreach (var e in entries)
        {
            if (e.HtmlRelativePath is null) continue;
            var escaped = HtmlReportRenderer.E(e.HtmlRelativePath);
            html = html.Replace(
                $"<td>{escaped}</td>",
                Loc.T($"<td><a href=\"{escaped}\">Mở &rarr;</a></td>", $"<td><a href=\"{escaped}\">Open &rarr;</a></td>"),
                StringComparison.Ordinal);
        }

        return html;
    }

    private static string VerdictLabel(VerdictLevel level) => level switch
    {
        VerdictLevel.Ok => Loc.T("Bình thường", "Normal"),
        VerdictLevel.Warning => Loc.T("Cần xem lại", "Needs review"),
        VerdictLevel.Bad => Loc.T("Có vấn đề", "Problem"),
        _ => Loc.T("Chưa xác định", "Undetermined")
    };
}

/// <summary>Mot bao cao tim thay khi quet, kem duong dan toi file HTML cua no.</summary>
public sealed record DashboardEntry(AuditReport Report, string? HtmlRelativePath);
