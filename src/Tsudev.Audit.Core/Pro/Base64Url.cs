namespace Tsudev.Audit.Core.Pro;

/// <summary>base64url khong dem (RFC 7515 muc 2). .NET 8 chua co System.Buffers.Text.Base64Url.</summary>
public static class Base64Url
{
    public static string Encode(ReadOnlySpan<byte> data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Giai ma; tra ve null neu khong phai base64url hop le (ke ca co dau '=' hay ky tu '+', '/').</summary>
    public static byte[]? TryDecode(string? text)
    {
        if (text is null) return null;
        foreach (var c in text)
            if (!(c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_'))
                return null;
        if (text.Length % 4 == 1) return null;
        var s = text.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        try { return Convert.FromBase64String(s); }
        catch (FormatException) { return null; }
    }
}
