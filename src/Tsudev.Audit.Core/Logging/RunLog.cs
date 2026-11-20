using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Tsudev.Audit.Core.Logging;

public enum RunLogLevel { Info, Warn, Error }

/// <summary>
/// Nhat ky co cau truc cua MOT lan chay (docs/STATE.md muc 3.5 muc 3) - JSON
/// Lines, moi dong mot doi tuong:
/// <c>{"ts":"2026-11-20T08:00:00.000Z","level":"info","event":"run.start",...}</c>.
///
/// Chi ghi khi nguoi dung chon <c>--log-file</c>: ky thuat vien gui tep nay khi
/// can ho tro tu xa, thay vi chup man hinh. Noi dung = dung nhung dong da in ra
/// man hinh + ma su kien, thoi luong tung buoc, ma thoat, ngoai le day du.
/// KHONG chep ket qua quet (serial, khoa san pham...) - nhung thu do o bao cao.
///
/// VI SAO KHONG DUNG Microsoft.Extensions.Logging (DECISIONS.md muc 4 hang 7 tung
/// goi y): them 4-5 goi vao ban single-file, lock file va THIRD-PARTY-NOTICES chi
/// de ghi mot tep. Ten su kien la hop dong on dinh cho may doc; <c>msg</c> theo
/// ngon ngu dang chay, chi de nguoi doc.
///
/// Loi ghi log KHONG BAO GIO lam hong lan quet: tep khong mo/ghi duoc ⇒ tat log.
/// </summary>
public sealed class RunLog : IDisposable
{
    public static RunLog Null { get; } = new(null, null);

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,   // giu nguyen tieng Viet de doc tay
    };

    private readonly object _gate = new();
    private readonly Func<DateTimeOffset> _clock;
    private TextWriter? _writer;

    public RunLog(TextWriter? writer, Func<DateTimeOffset>? clock = null)
    {
        _writer = writer;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public bool Enabled => _writer is not null;

    /// <summary>Mo tep (ghi NOI TIEP - nhieu lan chay chung mot tep van doc duoc). Loi ⇒ nem IOException/UnauthorizedAccessException.</summary>
    public static RunLog OpenFile(string path)
    {
        var full = Path.GetFullPath(path);
        if (Path.GetDirectoryName(full) is { Length: > 0 } dir) Directory.CreateDirectory(dir);
        var stream = new FileStream(full, FileMode.Append, FileAccess.Write, FileShare.Read);
        return new RunLog(new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true });
    }

    public void Info(string evt, string? msg = null, params (string Key, object? Value)[] fields)
        => Write(RunLogLevel.Info, evt, msg, fields);

    public void Warn(string evt, string? msg = null, params (string Key, object? Value)[] fields)
        => Write(RunLogLevel.Warn, evt, msg, fields);

    public void Error(string evt, string? msg = null, params (string Key, object? Value)[] fields)
        => Write(RunLogLevel.Error, evt, msg, fields);

    public void Write(RunLogLevel level, string evt, string? msg, params (string Key, object? Value)[] fields)
    {
        if (_writer is null) return;
        var line = Format(_clock(), level, evt, msg, fields);
        lock (_gate)
        {
            if (_writer is null) return;
            try { _writer.WriteLine(line); }
            catch (IOException) { _writer = null; }          // dia day, o rut ra... ⇒ tat log, quet tiep
            catch (ObjectDisposedException) { _writer = null; }
        }
    }

    /// <summary>Mot dong JSON. Tach ra de test khong can tep.</summary>
    public static string Format(DateTimeOffset ts, RunLogLevel level, string evt, string? msg,
                                IEnumerable<(string Key, object? Value)> fields)
    {
        using var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer, WriterOptions))
        {
            w.WriteStartObject();
            w.WriteString("ts", ts.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture));
            w.WriteString("level", level switch { RunLogLevel.Warn => "warn", RunLogLevel.Error => "error", _ => "info" });
            w.WriteString("event", evt);
            if (!string.IsNullOrEmpty(msg)) w.WriteString("msg", msg);
            foreach (var (key, value) in fields)
            {
                // Khoa co dinh khong duoc ghi de bang truong phu.
                if (key is "ts" or "level" or "event" or "msg") continue;
                w.WritePropertyName(key);
                WriteValue(w, value);
            }
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteValue(Utf8JsonWriter w, object? value)
    {
        switch (value)
        {
            case null: w.WriteNullValue(); break;
            case bool b: w.WriteBooleanValue(b); break;
            case int i: w.WriteNumberValue(i); break;
            case long l: w.WriteNumberValue(l); break;
            case double d when double.IsFinite(d): w.WriteNumberValue(Math.Round(d, 3)); break;
            case Enum e: w.WriteStringValue(e.ToString()); break;
            case IFormattable f: w.WriteStringValue(f.ToString(null, CultureInfo.InvariantCulture)); break;
            default: w.WriteStringValue(value.ToString()); break;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}
