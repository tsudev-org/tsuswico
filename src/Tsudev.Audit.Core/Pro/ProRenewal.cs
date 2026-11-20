using System.Globalization;
using Tsudev.Audit.Core.Localization;

namespace Tsudev.Audit.Core.Pro;

public enum ProRenewalKind { None, Reminder, Expired }

/// <summary>Vi sao hien / khong hien - de test noi ro ca nao dang xay ra.</summary>
public enum ProRenewalReason
{
    Reminder,
    Expired,
    NonInteractive,
    NoEntitlement,
    ResumedRecently,
    NotNearExpiry,
    NeverHadProHere,
    AlreadyNotified,
}

public sealed record ProRenewalDecision(ProRenewalKind Kind, ProRenewalReason Reason, int DaysLeft, long ProUntil)
{
    public bool Show => Kind != ProRenewalKind.None;
}

/// <summary>
/// Nhac gia han + bao het ky TsuDEV Pro - PRODUCT_POLICY v1.2.0 muc 7.6 (bo quy uoc v4.0.0), phieu 20261120-PRO2.
///
/// Nguon thoi han: <c>pro_until</c> cua JWS quyen loi CON HAN; khong co ⇒ khong hien gi.
/// <list type="bullet">
/// <item>Nhac: 0 &lt; <c>pro_until</c> - bay gio &lt;= 7 ngay, MOI LAN MO APP. Voi CLI, moi lan chay <c>swico.exe</c>
/// tuong tac la mot lan mo (tien trinh khong o nen) ⇒ <c>resumedAfter</c> = null. Tham so nay giu cho dung
/// dieu kien "quay lai sau &gt;= 30 phut o nen" cua chuan neu sau nay co giao dien chay nen.</item>
/// <item>Het ky: <c>pro_until</c> &lt;= bay gio, MAY NAY tung thay ky do con han (<see cref="SeenProUntilKey"/>),
/// bao MOT LAN cho moi <c>pro_until</c> (<see cref="NotifiedUntilKey"/>).</item>
/// </list>
/// <c>--silent</c>/<c>--json-only</c> (khong ai xem) MUST NOT hien - nhu muc 7.3.
/// </summary>
public static class ProRenewal
{
    /// <summary>Khoa chuan (muc 7.6): <c>pro_until</c> da bao het ky, giay Unix.</summary>
    public const string NotifiedUntilKey = "tsu.pro_expired.notified_until";

    /// <summary>Khoa rieng: <c>pro_until</c> gan nhat may nay thay CON HAN ("JWS truoc do cua thiet bi tung co Pro").</summary>
    public const string SeenProUntilKey = "tsu.pro.seen_until";

    public static readonly TimeSpan Window = TimeSpan.FromDays(7);
    public static readonly TimeSpan ResumeGap = TimeSpan.FromMinutes(30);

    /// <summary>Sau het ky, ban sao dam may con tai ve duoc 90 ngay (muc 7.2).</summary>
    public static readonly TimeSpan CloudGrace = TimeSpan.FromDays(90);

    public static ProRenewalDecision Decide(Entitlement? entitlement, long? seenProUntil, long? notifiedUntil,
                                            DateTimeOffset now, bool interactive, TimeSpan? resumedAfter = null)
    {
        if (!interactive) return None(ProRenewalReason.NonInteractive);
        if (entitlement is null) return None(ProRenewalReason.NoEntitlement);
        if (resumedAfter is { } gap && gap < ResumeGap) return None(ProRenewalReason.ResumedRecently);

        var until = entitlement.ProUntil;
        var left = until - now.ToUnixTimeSeconds();
        if (left > 0)
        {
            if (left > (long)Window.TotalSeconds) return None(ProRenewalReason.NotNearExpiry, until);
            var days = (int)Math.Max(1, Math.Ceiling(left / TimeSpan.FromDays(1).TotalSeconds));
            return new(ProRenewalKind.Reminder, ProRenewalReason.Reminder, days, until);
        }
        if (until <= 0 || seenProUntil != until) return None(ProRenewalReason.NeverHadProHere, until);
        if (notifiedUntil == until) return None(ProRenewalReason.AlreadyNotified, until);
        return new(ProRenewalKind.Expired, ProRenewalReason.Expired, 0, until);
    }

    private static ProRenewalDecision None(ProRenewalReason r, long until = 0) => new(ProRenewalKind.None, r, 0, until);

    /// <summary>Ngay theo gio may: DD/MM/YYYY (vi), theo ngon ngu (en).</summary>
    public static string DateText(long unixSeconds)
    {
        var local = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime();
        return Loc.IsEnglish
            ? local.ToString("MMMM d, yyyy", CultureInfo.GetCultureInfo("en-US"))
            : local.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    }

    // ---- Noi dung NGUYEN VAN muc 7.6 (khong sua tung chu) ----

    public static string ReminderTitle => Loc.T("Gia hạn TsuDEV Pro", "Renew TsuDEV Pro");

    public static string ReminderBody(int days, string date) => Loc.T(
        $"Gói TsuDEV Pro của bạn hết hạn sau {days} ngày (ngày {date}). Gia hạn để tiếp tục đồng bộ và sao lưu đám mây. Nếu không gia hạn, ứng dụng sẽ trở về bản miễn phí: dữ liệu trên thiết bị vẫn giữ nguyên nhưng không còn đồng bộ lên đám mây.",
        $"Your TsuDEV Pro expires in {days} days (on {date}). Renew to keep cloud sync and backup. If you don't renew, the app returns to the free version: data on this device stays intact but no longer syncs to the cloud.");

    public static string ExpiredTitle => Loc.T("TsuDEV Pro đã hết hạn", "TsuDEV Pro has expired");

    /// <summary>
    /// Tsu Swico CHUA co dong bo dam may (P4 cho TsuSync) ⇒ bo cau cuoi "Ban sao tren dam may..." dung nhu chuan cho
    /// phep ("San pham khong co dong bo dam may bo cau cuoi"). Co P4 thi truyen <paramref name="cloudDeleteDate"/>.
    /// </summary>
    public static string ExpiredBody(string? cloudDeleteDate = null)
    {
        var body = Loc.T(
            "Ứng dụng đã trở về bản miễn phí và ngừng đồng bộ lên đám mây. Dữ liệu trên thiết bị vẫn giữ nguyên.",
            "The app is back on the free version and has stopped syncing to the cloud. Data on this device stays intact.");
        if (cloudDeleteDate is null) return body;
        return body + Loc.T($" Bản sao trên đám mây còn tải về được tới ngày {cloudDeleteDate}.",
                            $" Your cloud copy can be downloaded until {cloudDeleteDate}.");
    }

    public static string RenewButton => Loc.T("Gia hạn", "Renew");
    public static string LaterButton => Loc.T("Để sau", "Later");
    public static string CloseButton => Loc.T("Đóng", "Close");

    // ---- Muc 7.5: dieu khoan dung TRUOC nut/lien ket mua (nguyen van) ----

    public static string Price => Loc.T("490.000 ₫", "490,000 ₫");

    public static string PurchaseTerms => Loc.T(
        $"TsuDEV Pro {Price} cho 1 năm, KHÔNG tự gia hạn. Gia hạn sớm được cộng thêm 1 năm vào thời hạn còn lại. Hết hạn mà chưa gia hạn, ứng dụng trở về bản miễn phí và ngừng đồng bộ lên đám mây; dữ liệu trên thiết bị vẫn giữ nguyên, bản sao trên đám mây còn tải về được 90 ngày rồi bị xoá.",
        $"TsuDEV Pro {Price} for 1 year, does NOT renew automatically. Renewing early adds 1 year to your remaining time. If it expires without renewal, the app returns to the free version and stops syncing to the cloud; data on your device stays intact, and your cloud copy can be downloaded for 90 days before it is deleted.");
}

public enum ProRenewalChoice { NotShown, Renew, Later }

/// <summary>
/// Hien nhac gia han / bao het ky o dau lan chay tuong tac. Khong chan quet: moi phim khac "1" (ke ca Enter,
/// het dau vao) la "De sau"/"Dong". Khong doi ma thoat. Ghi <see cref="ProRenewal.SeenProUntilKey"/> moi khi
/// thay Pro con han, va <see cref="ProRenewal.NotifiedUntilKey"/> TRUOC khi cho phim (tat ngang = da bao).
/// </summary>
public sealed class ProRenewalPrompt(
    IEntitlementSource entitlements,
    LocalStateStore state,
    string clientId,
    TextWriter output,
    Func<string?> readLine,
    Action<string> openUrl)
{
    public ProRenewalChoice Run(DateTimeOffset now, bool interactive)
    {
        if (!interactive) return ProRenewalChoice.NotShown;
        var ent = entitlements.Current(now);
        if (ent is not null && ent.IsProAt(now) && state.GetLong(ProRenewal.SeenProUntilKey) != ent.ProUntil)
            state.SetLong(ProRenewal.SeenProUntilKey, ent.ProUntil);

        var d = ProRenewal.Decide(ent, state.GetLong(ProRenewal.SeenProUntilKey),
                                  state.GetLong(ProRenewal.NotifiedUntilKey), now, interactive);
        if (!d.Show) return ProRenewalChoice.NotShown;

        output.WriteLine();
        if (d.Kind == ProRenewalKind.Reminder)
        {
            output.WriteLine($"=== {ProRenewal.ReminderTitle} ===");
            output.WriteLine(ProRenewal.ReminderBody(d.DaysLeft, ProRenewal.DateText(d.ProUntil)));
            output.WriteLine();
            output.WriteLine($"  [1] {ProRenewal.RenewButton}    [2] {ProRenewal.LaterButton}");
        }
        else
        {
            output.WriteLine($"=== {ProRenewal.ExpiredTitle} ===");
            output.WriteLine(ProRenewal.ExpiredBody());
            output.WriteLine();
            output.WriteLine($"  [1] {ProRenewal.RenewButton}    [2] {ProRenewal.CloseButton}");
            state.SetLong(ProRenewal.NotifiedUntilKey, d.ProUntil);
        }
        output.Write("> ");

        string? answer;
        try { answer = readLine(); }
        catch (IOException) { answer = null; }
        output.WriteLine();

        if (answer?.Trim() == "1")
        {
            openUrl(ProContract.ProUrl(clientId));
            return ProRenewalChoice.Renew;
        }
        return ProRenewalChoice.Later;
    }
}
