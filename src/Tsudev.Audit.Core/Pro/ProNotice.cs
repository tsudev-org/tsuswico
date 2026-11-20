using Tsudev.Audit.Core.Localization;

namespace Tsudev.Audit.Core.Pro;

/// <summary>Vi sao thong bao hien / khong hien - de test noi ro ca nao dang xay ra.</summary>
public enum ProNoticeReason
{
    Show,
    NonInteractive,
    NoEntitlement,
    HasPro,
    SingleDevice,
    ShownRecently,
}

public sealed record ProNoticeDecision(bool Show, ProNoticeReason Reason, int Devices);

/// <summary>
/// Thong bao "nhieu thiet bi chua co Pro" - PRODUCT_POLICY muc 7.3, ADR-0002 muc 2.4.
///
/// Dieu kien: chay TUONG TAC, co quyen loi hop le, khong co Pro con han,
/// <c>devices_30d</c> &gt;= 2, va lan hien gan nhat TREN MAY NAY da qua &gt;= 30 ngay.
/// <c>--silent</c>/<c>--json-only</c> (RMM/GPO, khong ai xem) MUST NOT hien.
/// </summary>
public static class ProNotice
{
    public const string LastShownKey = "tsu.pro_notice.last_shown";
    public static readonly TimeSpan Cycle = TimeSpan.FromDays(30);
    public const int MinDevices = 2;

    public static ProNoticeDecision Decide(Entitlement? entitlement, DateTimeOffset? lastShown,
                                           DateTimeOffset now, bool interactive)
    {
        if (!interactive) return new(false, ProNoticeReason.NonInteractive, 0);
        if (entitlement is null) return new(false, ProNoticeReason.NoEntitlement, 0);
        var n = entitlement.Devices30d;
        if (entitlement.IsProAt(now)) return new(false, ProNoticeReason.HasPro, n);
        if (n < MinDevices) return new(false, ProNoticeReason.SingleDevice, n);
        if (lastShown is { } last && now - last < Cycle) return new(false, ProNoticeReason.ShownRecently, n);
        return new(true, ProNoticeReason.Show, n);
    }

    // ---- Noi dung NGUYEN VAN muc 7.3 (khong sua tung chu) ----

    public static string Title => Loc.T("Dữ liệu chỉ lưu trên thiết bị này", "Your data stays on this device");

    public static string Body(int devices) => Loc.T(
        $"Tài khoản của bạn đang dùng trên {devices} thiết bị. Ở bản miễn phí, dữ liệu chỉ lưu trên từng thiết bị, không gửi lên internet hay đám mây, nên các thiết bị không tự đồng bộ với nhau. Để đồng bộ và sao lưu dữ liệu giữa các thiết bị, hãy nâng cấp lên TsuDEV Pro.",
        $"Your account is signed in on {devices} devices. On the free plan, data is stored only on each device and is never sent to the internet or the cloud, so your devices do not sync with each other. To sync and back up your data across devices, upgrade to TsuDEV Pro.");

    public static string PrimaryButton => Loc.T("Xem gói Pro", "See Pro plans");
    public static string SecondaryButton => Loc.T("Để sau", "Not now");
    public static string BackupLink => Loc.T("Sao lưu ra tệp (miễn phí)", "Back up to a file (free)");
}

public enum ProNoticeChoice { NotShown, SeePlans, NotNow }

/// <summary>
/// Hien thong bao o dau lan chay tuong tac (cung cho voi hop thoai cap nhat) va
/// ghi moc <see cref="ProNotice.LastShownKey"/>. Khong chan quet: moi phim khac
/// "1" (ke ca Enter, het dau vao) deu la "De sau". Khong doi ma thoat.
/// </summary>
public sealed class ProNoticePrompt(
    IEntitlementSource entitlements,
    LocalStateStore state,
    string clientId,
    TextWriter output,
    Func<string?> readLine,
    Action<string> openUrl)
{
    public ProNoticeChoice Run(DateTimeOffset now, bool interactive, string backupFolder)
    {
        var decision = ProNotice.Decide(entitlements.Current(now), state.GetTimestamp(ProNotice.LastShownKey),
                                        now, interactive);
        if (!decision.Show) return ProNoticeChoice.NotShown;

        output.WriteLine();
        output.WriteLine($"=== {ProNotice.Title} ===");
        output.WriteLine(ProNotice.Body(decision.Devices));
        output.WriteLine();
        output.WriteLine($"  [1] {ProNotice.PrimaryButton}    [2] {ProNotice.SecondaryButton}");
        output.WriteLine($"  {ProNotice.BackupLink}: {backupFolder}");
        output.Write("> ");

        // Ghi moc TRUOC khi cho phim: tat ngang cua so cung tinh la da hien.
        state.SetTimestamp(ProNotice.LastShownKey, now);

        string? answer;
        try { answer = readLine(); }
        catch (IOException) { answer = null; }
        output.WriteLine();

        if (answer?.Trim() == "1")
        {
            openUrl(ProContract.ProUrl(clientId));
            return ProNoticeChoice.SeePlans;
        }
        return ProNoticeChoice.NotNow;
    }
}
