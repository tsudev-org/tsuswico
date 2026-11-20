using Tsudev.Audit.Core.Cli;
using Tsudev.Audit.Core.Localization;
using Tsudev.Audit.Core.Pro;

namespace Tsudev.Audit.Tests;

/// <summary>
/// PRODUCT_POLICY v1.2.0 muc 7.6 (bo quy uoc v4.0.0) - nhac gia han 7 ngay cuoi + bao het ky mot lan. Sau ca dau
/// la 6 TEST BAT BUOC cua muc 7.6; phan sau: --silent/--json-only, noi dung nguyen van 7.5/7.6 vi/en.
/// </summary>
public class ProRenewalTests
{
    private static readonly DateTimeOffset Now = ProTestKit.Now;

    private static long Days(double d) => Now.AddDays(d).ToUnixTimeSeconds();

    private static ProRenewalPrompt Prompt(IEntitlementSource src, LocalStateStore state, StringWriter output,
                                           string? answer = "2", Action<string>? open = null)
        => new(src, state, ProTestKit.ClientId, output, () => answer, open ?? (_ => { }));

    // ---- 6 test bat buoc (muc 7.6) ----

    [Fact]
    public void Bat_buoc_1_con_8_ngay_khong_nhac()
    {
        using var kit = new ProTestKit();
        var d = ProRenewal.Decide(kit.Ent(proUntil: Days(8)), null, null, Now, interactive: true);
        Assert.False(d.Show);
        Assert.Equal(ProRenewalReason.NotNearExpiry, d.Reason);
        // Bien: con 7 ngay + 1 giay van chua nhac.
        Assert.False(ProRenewal.Decide(kit.Ent(proUntil: Days(7) + 1), null, null, Now, true).Show);
    }

    [Fact]
    public void Bat_buoc_2_con_7_ngay_nhac()
    {
        using var kit = new ProTestKit();
        var d = ProRenewal.Decide(kit.Ent(proUntil: Days(7)), null, null, Now, interactive: true);
        Assert.Equal(ProRenewalKind.Reminder, d.Kind);
        Assert.Equal(7, d.DaysLeft);
        // {n} lam tron len, toi thieu 1.
        Assert.Equal(1, ProRenewal.Decide(kit.Ent(proUntil: Now.ToUnixTimeSeconds() + 60), null, null, Now, true).DaysLeft);
        Assert.Equal(3, ProRenewal.Decide(kit.Ent(proUntil: Days(2.1)), null, null, Now, true).DaysLeft);
    }

    [Fact]
    public void Bat_buoc_3_mo_2_lan_nhac_2_lan_quay_lai_duoi_30_phut_khong()
    {
        using var kit = new ProTestKit();
        using var dir = new TempDir();
        var state = new LocalStateStore(dir.Path);
        var src = new FakeEntitlementSource(kit.Ent(proUntil: Days(5)));

        Assert.Equal(ProRenewalChoice.Later, Prompt(src, state, new StringWriter()).Run(Now, true));
        Assert.Equal(ProRenewalChoice.Later, Prompt(src, state, new StringWriter()).Run(Now.AddMinutes(1), true));

        var ent = src.Current(Now);
        Assert.Equal(ProRenewalReason.ResumedRecently,
                     ProRenewal.Decide(ent, null, null, Now, true, resumedAfter: TimeSpan.FromMinutes(29)).Reason);
        Assert.True(ProRenewal.Decide(ent, null, null, Now, true, resumedAfter: TimeSpan.FromMinutes(30)).Show);
    }

    [Fact]
    public void Bat_buoc_4_het_han_bao_mot_lan_mo_lai_khong()
    {
        using var kit = new ProTestKit();
        using var dir = new TempDir();
        var state = new LocalStateStore(dir.Path);
        var until = Days(1);

        // May nay thay Pro con han (nhac 1 ngay cuoi)...
        Prompt(new FakeEntitlementSource(kit.Ent(proUntil: until)), state, new StringWriter()).Run(Now, true);
        Assert.Equal(until, state.GetLong(ProRenewal.SeenProUntilKey));

        // ...hai ngay sau JWS moi van ghi pro_until cu (da qua) ⇒ bao het ky dung mot lan.
        var later = Now.AddDays(2);
        var expired = new FakeEntitlementSource(kit.Ent(proUntil: until));
        var out1 = new StringWriter();
        Assert.Equal(ProRenewalChoice.Later, Prompt(expired, state, out1).Run(later, true));
        Assert.Contains(ProRenewal.ExpiredTitle, out1.ToString(), StringComparison.Ordinal);
        Assert.Equal(until, state.GetLong(ProRenewal.NotifiedUntilKey));

        var out2 = new StringWriter();
        Assert.Equal(ProRenewalChoice.NotShown, Prompt(expired, state, out2).Run(later.AddHours(1), true));
        Assert.Equal("", out2.ToString());
    }

    [Fact]
    public void Bat_buoc_5_gia_han_thoi_nhac_ky_sau_bao_lai()
    {
        using var kit = new ProTestKit();
        using var dir = new TempDir();
        var state = new LocalStateStore(dir.Path);
        var first = Days(3);

        Assert.Equal(ProRenewalChoice.Later,
                     Prompt(new FakeEntitlementSource(kit.Ent(proUntil: first)), state, new StringWriter()).Run(Now, true));

        // Gia han som: pro_until = cu + 1 nam ⇒ thoi nhac.
        var renewed = first + (long)TimeSpan.FromDays(365).TotalSeconds;
        var renewedSrc = new FakeEntitlementSource(kit.Ent(proUntil: renewed));
        Assert.Equal(ProRenewalChoice.NotShown, Prompt(renewedSrc, state, new StringWriter()).Run(Now.AddDays(1), true));

        // Ky moi het (khong gia han nua) ⇒ bao het ky lai, du ky truoc da tung "bao".
        state.SetLong(ProRenewal.NotifiedUntilKey, first);
        var after = DateTimeOffset.FromUnixTimeSeconds(renewed).AddDays(1);
        var o = new StringWriter();
        Assert.Equal(ProRenewalChoice.Later, Prompt(renewedSrc, state, o).Run(after, true));
        Assert.Contains(ProRenewal.ExpiredTitle, o.ToString(), StringComparison.Ordinal);
        Assert.Equal(renewed, state.GetLong(ProRenewal.NotifiedUntilKey));
    }

    [Fact]
    public void Bat_buoc_6_ngoai_tuyen_JWS_het_han_khong_nhac_khong_loi()
    {
        using var kit = new ProTestKit();
        // JWS dem tu 8 ngay truoc (song 7 ngay) ⇒ qua exp; pro_until con 2 ngay nhung khong co JWS con han.
        var stale = kit.Sign(ProTestKit.Claims(proUntil: Days(2), iat: Now.AddDays(-8)));
        var src = new CachedEntitlementSource(stale, kit.Verifier());
        using var dir = new TempDir();
        var output = new StringWriter();
        var prompt = new ProRenewalPrompt(src, new LocalStateStore(dir.Path), ProTestKit.ClientId, output,
                                          () => throw new InvalidOperationException("khong duoc hoi"), _ => { });

        Assert.Equal(ProRenewalChoice.NotShown, prompt.Run(Now, interactive: true));
        Assert.Equal("", output.ToString());
        Assert.Equal(ProRenewalReason.NoEntitlement, ProRenewal.Decide(src.Current(Now), null, null, Now, true).Reason);
    }

    // ---- Cac ca bo sung ----

    [Fact]
    public void Chua_tung_thay_Pro_tren_may_nay_thi_khong_bao_het_ky()
    {
        using var kit = new ProTestKit();
        // Tai khoan co Pro tu nam ngoai, dang nhap may moi: JWS dau tien da het ky ⇒ khong "bao het ky".
        var d = ProRenewal.Decide(kit.Ent(proUntil: Days(-300)), seenProUntil: null, notifiedUntil: null, Now, true);
        Assert.Equal(ProRenewalReason.NeverHadProHere, d.Reason);
        Assert.False(ProRenewal.Decide(kit.Ent(proUntil: 0), 0, null, Now, true).Show);
    }

    [Theory]
    [InlineData("--silent")]
    [InlineData("--json-only")]
    public void Khong_hien_o_silent_va_json_only(string flag)
    {
        using var kit = new ProTestKit();
        var opts = CliOptions.Parse([flag]);
        using var dir = new TempDir();
        var output = new StringWriter();
        var prompt = new ProRenewalPrompt(new FakeEntitlementSource(kit.Ent(proUntil: Days(2))),
                                          new LocalStateStore(dir.Path), ProTestKit.ClientId, output,
                                          () => throw new InvalidOperationException("khong duoc cho phim"), _ => { });

        Assert.Equal(ProRenewalChoice.NotShown, prompt.Run(Now, interactive: !opts.Silent && !opts.JsonOnly));
        Assert.Equal("", output.ToString());
        Assert.False(File.Exists(Path.Combine(dir.Path, LocalStateStore.FileName)));
    }

    [Fact]
    public void Chon_1_mo_trang_Pro()
    {
        using var kit = new ProTestKit();
        using var dir = new TempDir();
        string? opened = null;
        var prompt = new ProRenewalPrompt(new FakeEntitlementSource(kit.Ent(proUntil: Days(2))),
                                          new LocalStateStore(dir.Path), "tsuswico", new StringWriter(),
                                          () => "1", u => opened = u);
        Assert.Equal(ProRenewalChoice.Renew, prompt.Run(Now, true));
        Assert.Equal("https://tsudev.com/pro?app=tsuswico", opened);
    }

    [Fact]
    public void Noi_dung_nguyen_van_vi()
    {
        using var _ = Loc.Use(AppLanguage.Vi);
        Assert.Equal("Gia hạn TsuDEV Pro", ProRenewal.ReminderTitle);
        Assert.Equal("Gói TsuDEV Pro của bạn hết hạn sau 3 ngày (ngày 23/11/2026). Gia hạn để tiếp tục đồng bộ và sao lưu đám mây. Nếu không gia hạn, ứng dụng sẽ trở về bản miễn phí: dữ liệu trên thiết bị vẫn giữ nguyên nhưng không còn đồng bộ lên đám mây.",
                     ProRenewal.ReminderBody(3, "23/11/2026"));
        Assert.Equal("TsuDEV Pro đã hết hạn", ProRenewal.ExpiredTitle);
        Assert.Equal("Ứng dụng đã trở về bản miễn phí và ngừng đồng bộ lên đám mây. Dữ liệu trên thiết bị vẫn giữ nguyên.",
                     ProRenewal.ExpiredBody());
        Assert.EndsWith(" Bản sao trên đám mây còn tải về được tới ngày 18/02/2027.", ProRenewal.ExpiredBody("18/02/2027"),
                        StringComparison.Ordinal);
        Assert.Equal(("Gia hạn", "Để sau", "Đóng"), (ProRenewal.RenewButton, ProRenewal.LaterButton, ProRenewal.CloseButton));
        Assert.Equal("TsuDEV Pro 490.000 ₫ cho 1 năm, KHÔNG tự gia hạn. Gia hạn sớm được cộng thêm 1 năm vào thời hạn còn lại. Hết hạn mà chưa gia hạn, ứng dụng trở về bản miễn phí và ngừng đồng bộ lên đám mây; dữ liệu trên thiết bị vẫn giữ nguyên, bản sao trên đám mây còn tải về được 90 ngày rồi bị xoá.",
                     ProRenewal.PurchaseTerms);
    }

    [Fact]
    public void Noi_dung_nguyen_van_en()
    {
        using var _ = Loc.Use(AppLanguage.En);
        Assert.Equal("Renew TsuDEV Pro", ProRenewal.ReminderTitle);
        Assert.Equal("Your TsuDEV Pro expires in 3 days (on November 23, 2026). Renew to keep cloud sync and backup. If you don't renew, the app returns to the free version: data on this device stays intact but no longer syncs to the cloud.",
                     ProRenewal.ReminderBody(3, "November 23, 2026"));
        Assert.Equal("TsuDEV Pro has expired", ProRenewal.ExpiredTitle);
        Assert.Equal("The app is back on the free version and has stopped syncing to the cloud. Data on this device stays intact.",
                     ProRenewal.ExpiredBody());
        Assert.Equal(("Renew", "Later", "Close"), (ProRenewal.RenewButton, ProRenewal.LaterButton, ProRenewal.CloseButton));
        Assert.Equal("TsuDEV Pro 490,000 ₫ for 1 year, does NOT renew automatically. Renewing early adds 1 year to your remaining time. If it expires without renewal, the app returns to the free version and stops syncing to the cloud; data on your device stays intact, and your cloud copy can be downloaded for 90 days before it is deleted.",
                     ProRenewal.PurchaseTerms);
    }

    [Fact]
    public void Dieu_khoan_7_5_khop_nguyen_van_ban_quy_uoc_dong_bo()
    {
        var policy = File.ReadAllText(Path.Combine(RepoRoot(), ".standards", "docs", "PRODUCT_POLICY.md"))
            .Replace("\n  - (en): ", " ", StringComparison.Ordinal).Replace("\n  ", " ", StringComparison.Ordinal);
        using (Loc.Use(AppLanguage.Vi))
            Assert.Contains(ProRenewal.PurchaseTerms.Replace("490.000 ₫", "{giá}", StringComparison.Ordinal), policy, StringComparison.Ordinal);
        using (Loc.Use(AppLanguage.En))
            Assert.Contains(ProRenewal.PurchaseTerms.Replace("490,000 ₫", "{price}", StringComparison.Ordinal), policy, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, ".standards"))) d = d.Parent;
        return d?.FullName ?? throw new DirectoryNotFoundException(".standards");
    }
}
