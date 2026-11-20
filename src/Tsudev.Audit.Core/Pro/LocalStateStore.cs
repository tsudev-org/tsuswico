using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tsudev.Audit.Core.Pro;

/// <summary>
/// <c>state.json</c> trong thu muc du lieu cuc bo (ADR-0002 muc 2.4). Giu nguyen
/// moi khoa khong biet; tep hong ⇒ coi nhu rong (khong bao gio lam hong lan quet).
/// Chi DOC thi khong tao thu muc/tep nao - ban mien phi chua dang nhap khong de
/// lai dau vet tren may.
/// </summary>
public sealed class LocalStateStore(string directory)
{
    public const string FileName = "state.json";

    public string FilePath => Path.Combine(directory, FileName);

    /// <summary>Doc moc thoi gian UTC ISO 8601; thieu hoac sai dinh dang ⇒ null.</summary>
    public DateTimeOffset? GetTimestamp(string key)
    {
        var value = Load()[key];
        if (value is JsonValue v && v.TryGetValue<string>(out var s) &&
            DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var t))
            return t;
        return null;
    }

    /// <summary>Ghi moc UTC dang <c>yyyy-MM-ddTHH:mm:ssZ</c>; ghi qua tep tam roi doi ten.</summary>
    public void SetTimestamp(string key, DateTimeOffset value)
        => Write(key, value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));

    /// <summary>Doc so nguyen (vd. giay Unix); thieu hoac sai kieu ⇒ null.</summary>
    public long? GetLong(string key)
        => Load()[key] is JsonValue v && v.TryGetValue<long>(out var n) ? n : null;

    public void SetLong(string key, long value) => Write(key, value);

    private void Write(string key, JsonNode value)
    {
        var root = Load();
        root[key] = value;
        Directory.CreateDirectory(directory);
        var tmp = FilePath + ".tmp";
        File.WriteAllText(tmp, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tmp, FilePath, overwrite: true);
    }

    private JsonObject Load()
    {
        try
        {
            if (File.Exists(FilePath) && JsonNode.Parse(File.ReadAllText(FilePath)) is JsonObject o)
                return o;
        }
        catch (JsonException) { }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return new JsonObject();
    }
}
