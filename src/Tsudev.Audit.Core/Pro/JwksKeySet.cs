using System.Security.Cryptography;
using System.Text.Json;

namespace Tsudev.Audit.Core.Pro;

/// <summary>
/// Bo khoa cong khai lay tu <see cref="ProContract.JwksUrl"/>, chon khoa theo <c>kid</c>.
/// Chi giu khoa EC P-256 (ES256); khoa loai khac bi bo qua thay vi lam hong ca bo.
/// </summary>
public sealed class JwksKeySet : IDisposable
{
    private readonly Dictionary<string, ECDsa> _keys = new(StringComparer.Ordinal);

    private JwksKeySet() { }

    public int Count => _keys.Count;

    public ECDsa? Find(string kid) => _keys.GetValueOrDefault(kid);

    /// <summary>Doc JSON <c>{"keys":[...]}</c>. JSON hong ⇒ <see cref="FormatException"/>.</summary>
    public static JwksKeySet Parse(string json)
    {
        var set = new JwksKeySet();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("keys", out var keys) || keys.ValueKind != JsonValueKind.Array)
                throw new FormatException("JWKS thieu mang 'keys'.");

            foreach (var k in keys.EnumerateArray())
            {
                if (k.ValueKind != JsonValueKind.Object) continue;
                if (Str(k, "kty") != "EC" || Str(k, "crv") != "P-256") continue;
                if (Str(k, "alg") is { } alg && alg != ProContract.Algorithm) continue;
                if (Str(k, "use") is { } use && use != "sig") continue;
                var kid = Str(k, "kid");
                var x = Base64Url.TryDecode(Str(k, "x"));
                var y = Base64Url.TryDecode(Str(k, "y"));
                if (kid is null || x is not { Length: 32 } || y is not { Length: 32 } || set._keys.ContainsKey(kid))
                    continue;

                ECDsa ecdsa;
                try
                {
                    // ImportParameters kiem diem co nam tren duong cong hay khong.
                    ecdsa = ECDsa.Create(new ECParameters
                    {
                        Curve = ECCurve.NamedCurves.nistP256,
                        Q = new ECPoint { X = x, Y = y },
                    });
                }
                catch (CryptographicException) { continue; }
                set._keys[kid] = ecdsa;
            }
        }
        catch (JsonException ex)
        {
            set.Dispose();
            throw new FormatException("JWKS khong phai JSON hop le.", ex);
        }
        catch
        {
            set.Dispose();
            throw;
        }
        return set;
    }

    private static string? Str(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    public void Dispose()
    {
        foreach (var k in _keys.Values) k.Dispose();
        _keys.Clear();
    }
}
