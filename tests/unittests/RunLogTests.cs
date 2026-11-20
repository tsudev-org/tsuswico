using System.Text.Json;
using Tsudev.Audit.Core.Logging;

namespace Tsudev.Audit.Tests;

/// <summary>Nhat ky co cau truc (docs/STATE.md 3.5 muc 3): JSON Lines, chi bat bang --log-file.</summary>
public class RunLogTests
{
    private static readonly DateTimeOffset T = new(2026, 11, 20, 15, 0, 0, 250, TimeSpan.FromHours(7));

    [Fact]
    public void Moi_dong_la_mot_doi_tuong_JSON_voi_khoa_co_dinh()
    {
        var line = RunLog.Format(T, RunLogLevel.Warn, "step.fail", "WMI hỏng", [("step", "CPU"), ("ms", 1234L)]);
        using var doc = JsonDocument.Parse(line);
        var r = doc.RootElement;
        Assert.Equal("2026-11-20T08:00:00.250Z", r.GetProperty("ts").GetString());   // luon UTC
        Assert.Equal("warn", r.GetProperty("level").GetString());
        Assert.Equal("step.fail", r.GetProperty("event").GetString());
        Assert.Equal("WMI hỏng", r.GetProperty("msg").GetString());
        Assert.Equal(1234, r.GetProperty("ms").GetInt64());
        Assert.DoesNotContain('\n', line);
        Assert.Contains("hỏng", line);   // tieng Viet giu nguyen, khong \u1ecf
    }

    [Fact]
    public void Kieu_truong_phu_va_khong_cho_ghi_de_khoa_co_dinh()
    {
        var line = RunLog.Format(T, RunLogLevel.Info, "x", null,
            [("b", true), ("n", null), ("d", 1.23456), ("e", AuditScope.License), ("event", "gia-mao"), ("dt", T)]);
        using var doc = JsonDocument.Parse(line);
        var r = doc.RootElement;
        Assert.True(r.GetProperty("b").GetBoolean());
        Assert.Equal(JsonValueKind.Null, r.GetProperty("n").ValueKind);
        Assert.Equal(1.235, r.GetProperty("d").GetDouble());
        Assert.Equal("License", r.GetProperty("e").GetString());
        Assert.Equal("x", r.GetProperty("event").GetString());
        Assert.False(r.TryGetProperty("msg", out _));
        Assert.Single(r.EnumerateObject(), p => p.Name == "event");
    }

    [Fact]
    public void Ky_tu_dieu_khien_va_dau_ngoac_kep_khong_lam_vo_dong()
    {
        var line = RunLog.Format(T, RunLogLevel.Error, "run.fatal", "a\"b\nc\r\td", []);
        Assert.DoesNotContain('\n', line);
        using var doc = JsonDocument.Parse(line);
        Assert.Equal("a\"b\nc\r\td", doc.RootElement.GetProperty("msg").GetString());
    }

    [Fact]
    public void Null_khong_ghi_gi_va_khong_nem_loi()
    {
        Assert.False(RunLog.Null.Enabled);
        RunLog.Null.Info("x", "y", ("a", 1));
    }

    [Fact]
    public void Ghi_tep_noi_tiep_qua_nhieu_lan_chay()
    {
        using var dir = new TempDir();
        var file = Path.Combine(dir.Path, "con", "swico.jsonl");   // thu muc chua co ⇒ tu tao
        using (var log = RunLog.OpenFile(file)) log.Info("run.start");
        using (var log = RunLog.OpenFile(file)) log.Info("run.start");
        var lines = File.ReadAllLines(file);
        Assert.Equal(2, lines.Length);
        Assert.All(lines, l => Assert.Equal("run.start", JsonDocument.Parse(l).RootElement.GetProperty("event").GetString()));
        Assert.NotEqual(0xEF, File.ReadAllBytes(file)[0]);   // khong BOM: moi dong deu la JSON hop le
    }

    [Fact]
    public void Writer_hong_thi_tat_log_khong_lam_hong_lan_quet()
    {
        var log = new RunLog(new BrokenWriter(), () => T);
        log.Info("a");
        Assert.False(log.Enabled);
        log.Info("b");   // khong nem
    }

    [Fact]
    public void Bo_ghi_tien_trinh_chuyen_tiep_nguyen_va_ghi_thoi_luong()
    {
        var sw = new StringWriter();
        var inner = new RecordingProgressSink();
        var sink = new LoggingProgressSink(inner, new RunLog(sw, () => T));

        sink.BeginStep("CPU"); sink.EndStep();
        sink.BeginStep("Office"); sink.Note("ghi chú"); sink.FailStep("lỗi COM");

        Assert.Equal(new[] { "BEGIN CPU", "END", "BEGIN Office", "NOTE ghi chú", "FAIL lỗi COM" }, inner.Events);
        var lines = sw.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
                      .Select(l => JsonDocument.Parse(l).RootElement).ToList();
        Assert.Equal(new[] { "step.ok", "note", "step.fail" }, lines.Select(l => l.GetProperty("event").GetString()));
        Assert.Equal("CPU", lines[0].GetProperty("step").GetString());
        Assert.True(lines[0].GetProperty("ms").GetInt64() >= 0);
        Assert.Equal("Office", lines[2].GetProperty("step").GetString());
        Assert.Equal("lỗi COM", lines[2].GetProperty("msg").GetString());
    }

    [Fact]
    public void Tham_so_log_file()
    {
        Assert.Null(CliOptions.Parse([]).LogFile);
        Assert.Equal("C:\\x\\a.jsonl", CliOptions.Parse(["--log-file", "C:\\x\\a.jsonl"]).LogFile);
        Assert.Throws<ArgumentException>(() => CliOptions.Parse(["--log-file"]));
        Assert.Throws<ArgumentException>(() => CliOptions.Parse(["--log-file", " "]));
        foreach (var lang in new[] { AppLanguage.Vi, AppLanguage.En })
        {
            using var _ = Loc.Use(lang);
            Assert.Contains("--log-file", CliOptions.UsageText);
        }
    }

    private sealed class BrokenWriter : StringWriter
    {
        public override void WriteLine(string? value) => throw new IOException("đĩa đầy");
    }
}
