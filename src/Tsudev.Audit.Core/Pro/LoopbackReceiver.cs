using System.Net;
using System.Net.Sockets;
using System.Text;
using Tsudev.Audit.Core.Localization;

namespace Tsudev.Audit.Core.Pro;

/// <summary>Tham so ma tsudev.com tra ve <c>/callback</c> (RFC 6749 4.1.2, RFC 9207 <c>iss</c>).</summary>
public sealed record AuthorizationResponse(string? Code, string? State, string? Error, string? ErrorDescription,
                                           string? Issuer)
{
    /// <summary>Query sai dang (lap khoa, ma % hong): coi nhu loi, khong doan.</summary>
    public static AuthorizationResponse Malformed { get; } = new(null, null, "invalid_request", null, null);

    public static AuthorizationResponse Parse(string? query)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in (query ?? "").TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=', StringComparison.Ordinal);
            string key, value;
            try
            {
                key = Uri.UnescapeDataString((eq < 0 ? pair : pair[..eq]).Replace('+', ' '));
                value = eq < 0 ? "" : Uri.UnescapeDataString(pair[(eq + 1)..].Replace('+', ' '));
            }
            catch (UriFormatException) { return Malformed; }
            // RFC 6749 3.1: tham so KHONG duoc lap. Hai gia tri `code`/`state` la dau hieu bi chen.
            if (!map.TryAdd(key, value)) return Malformed;
        }
        return new AuthorizationResponse(map.GetValueOrDefault("code"), map.GetValueOrDefault("state"),
                                         map.GetValueOrDefault("error"), map.GetValueOrDefault("error_description"),
                                         map.GetValueOrDefault("iss"));
    }
}

/// <summary>
/// Nhan chuyen huong OIDC tren loopback (RFC 8252 muc 7.3): <c>http://127.0.0.1:&lt;cong ngau nhien&gt;/callback</c>,
/// duong lui <c>http://[::1]:...</c> khi may khong co IPv4 loopback.
///
/// VI SAO TcpListener ma khong HttpListener: tren Windows HttpListener di qua http.sys - tien
/// to <c>127.0.0.1</c> can URL ACL/quyen quan tri, con <c>localhost</c> thi tsudev.com tu choi
/// (no phan giai duoc ra ngoai loopback). Socket chi nghe dia chi loopback, cong do he dieu hanh
/// cap (port 0), mot yeu cau GET la du - khong can may chu HTTP day du.
/// </summary>
public sealed class LoopbackReceiver : IDisposable
{
    private const int MaxRequestHead = 8192;
    private readonly TcpListener _listener;

    public string RedirectUri { get; }

    private LoopbackReceiver(TcpListener listener, string host)
    {
        _listener = listener;
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        RedirectUri = $"http://{host}:{port.ToString(System.Globalization.CultureInfo.InvariantCulture)}{ProContract.CallbackPath}";
    }

    /// <summary>Mo cong tren 127.0.0.1; khong duoc thi [::1]. Ca hai deu hong ⇒ <see cref="SocketException"/>.</summary>
    public static LoopbackReceiver Start(bool preferIPv6 = false)
    {
        var order = preferIPv6
            ? new[] { (IPAddress.IPv6Loopback, "[::1]"), (IPAddress.Loopback, "127.0.0.1") }
            : new[] { (IPAddress.Loopback, "127.0.0.1"), (IPAddress.IPv6Loopback, "[::1]") };
        SocketException? last = null;
        foreach (var (address, host) in order)
        {
            var l = new TcpListener(address, 0);
            try
            {
                l.Start();
                return new LoopbackReceiver(l, host);
            }
            catch (SocketException ex) { last = ex; l.Stop(); }
        }
        throw last!;
    }

    /// <summary>
    /// Cho den khi co GET <c>/callback</c>. Duong khac (vd <c>/favicon.ico</c>) ⇒ 404 va cho tiep.
    /// Het <paramref name="timeout"/> ⇒ <see cref="TimeoutException"/>.
    /// </summary>
    public async Task<AuthorizationResponse> WaitAsync(TimeSpan timeout, CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        cts.CancelAfter(timeout);
        try
        {
            while (true)
            {
                using var client = await _listener.AcceptTcpClientAsync(cts.Token).ConfigureAwait(false);
                var result = await HandleAsync(client, cts.Token).ConfigureAwait(false);
                if (result is not null) return result;
            }
        }
        catch (OperationCanceledException) when (!cancel.IsCancellationRequested)
        {
            throw new TimeoutException("loopback callback timeout");
        }
    }

    private static async Task<AuthorizationResponse?> HandleAsync(TcpClient client, CancellationToken cancel)
    {
        var stream = client.GetStream();
        string head;
        using (var perConnection = CancellationTokenSource.CreateLinkedTokenSource(cancel))
        {
            // Mot ket noi mo ma khong gui gi khong duoc giu chan ca luot dang nhap.
            perConnection.CancelAfter(TimeSpan.FromSeconds(10));
            try { head = await ReadHeadAsync(stream, perConnection.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (!cancel.IsCancellationRequested) { return null; }
            catch (IOException) { return null; }
        }

        var line = head.Split("\r\n", 2)[0].Split(' ');
        if (line.Length != 3 || !line[2].StartsWith("HTTP/1.", StringComparison.Ordinal))
        {
            await RespondAsync(stream, 400, "Bad Request", Page(false), cancel).ConfigureAwait(false);
            return null;
        }
        var target = line[1];
        var q = target.IndexOf('?', StringComparison.Ordinal);
        var path = q < 0 ? target : target[..q];
        if (line[0] != "GET" || path != ProContract.CallbackPath)
        {
            await RespondAsync(stream, 404, "Not Found", "", cancel).ConfigureAwait(false);
            return null;
        }

        var response = AuthorizationResponse.Parse(q < 0 ? "" : target[(q + 1)..]);
        await RespondAsync(stream, 200, "OK", Page(response.Error is null && response.Code is not null), cancel)
            .ConfigureAwait(false);
        return response;
    }

    private static async Task<string> ReadHeadAsync(NetworkStream stream, CancellationToken cancel)
    {
        var buffer = new byte[MaxRequestHead];
        var length = 0;
        while (length < buffer.Length)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(length), cancel).ConfigureAwait(false);
            if (n == 0) break;
            length += n;
            if (buffer.AsSpan(0, length).IndexOf("\r\n\r\n"u8) >= 0) break;
        }
        return Encoding.ASCII.GetString(buffer, 0, length);
    }

    private static async Task RespondAsync(NetworkStream stream, int status, string reason, string html,
                                           CancellationToken cancel)
    {
        var body = Encoding.UTF8.GetBytes(html);
        var header =
            $"HTTP/1.1 {status.ToString(System.Globalization.CultureInfo.InvariantCulture)} {reason}\r\n" +
            "Content-Type: text/html; charset=utf-8\r\n" +
            $"Content-Length: {body.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)}\r\n" +
            "Cache-Control: no-store\r\n" +
            "Referrer-Policy: no-referrer\r\n" +
            "Content-Security-Policy: default-src 'none'\r\n" +
            "X-Content-Type-Options: nosniff\r\n" +
            "Connection: close\r\n\r\n";
        try
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes(header), cancel).ConfigureAwait(false);
            await stream.WriteAsync(body, cancel).ConfigureAwait(false);
            await stream.FlushAsync(cancel).ConfigureAwait(false);
        }
        catch (IOException) { /* trinh duyet dong tab som - khong anh huong ket qua */ }
    }

    /// <summary>Trang tinh, khong script, khong tai gi ngoai (CSP <c>default-src 'none'</c>).</summary>
    private static string Page(bool ok)
    {
        var title = ok ? Loc.T("Đã nhận phản hồi đăng nhập", "Sign-in response received")
                       : Loc.T("Đăng nhập chưa hoàn tất", "Sign-in not completed");
        var text = ok
            ? Loc.T("Hãy quay lại cửa sổ Tsu Swico để xem kết quả. Bạn có thể đóng thẻ này.",
                    "Go back to the Tsu Swico window to see the result. You can close this tab.")
            : Loc.T("Quay lại cửa sổ Tsu Swico để xem lý do, rồi chạy lại swico login.",
                    "Go back to the Tsu Swico window for the reason, then run swico login again.");
        return $"<!doctype html><html lang=\"{Loc.HtmlLang}\"><meta charset=\"utf-8\"><title>{title} - Tsu Swico</title>" +
               $"<body><h1>{title}</h1><p>{text}</p></body></html>";
    }

    public void Dispose() => _listener.Stop();
}
