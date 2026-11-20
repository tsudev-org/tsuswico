using Tsudev.Audit.Core.Pro;

namespace Tsudev.Audit.Tests;

/// <summary>
/// PRODUCT_POLICY 7.3 - thong bao "nhieu thiet bi chua co Pro". Nam ca dau la
/// 5 TEST BAT BUOC cua muc 7.3 (nguon quyen loi gia); phan sau la ADR-0002 muc 2.4
/// (khong hien o --silent/--json-only) va noi dung nguyen van vi/en.
/// </summary>
public class ProNoticeTests
{
    private static readonly DateTimeOffset Now = ProTestKit.Now;

    // ---- 5 test bat buoc (muc 7.3) ----

    [Fact]
    public void Bat_buoc_1_mot_thiet_bi_khong_hien()
    {
        using var kit = new ProTestKit();
        var d = ProNotice.Decide(kit.Ent(devices: 1), null, Now, interactive: true);
        Assert.False(d.Show);
        Assert.Equal(ProNoticeReason.SingleDevice, d.Reason);
    }

    [Fact]
    public void Bat_buoc_2_hai_thiet_bi_hien()
    {
        using var kit = new ProTestKit();
        var d = ProNotice.Decide(kit.Ent(devices: 2), null, Now, interactive: true);
        Assert.True(d.Show);
        Assert.Equal(2, d.Devices);
    }

    [Fact]
    public void Bat_buoc_3_hien_ngay_0_thi_ngay_29_khong_ngay_30_hien_lai()
    {
        using var kit = new ProTestKit();
        var ent = kit.Ent(devices: 2);
        var day0 = Now;
        Assert.True(ProNotice.Decide(ent, null, day0, true).Show);
        var d29 = ProNotice.Decide(ent, day0, day0.AddDays(29), true);
        Assert.False(d29.Show);
        Assert.Equal(ProNoticeReason.ShownRecently, d29.Reason);
        Assert.False(ProNotice.Decide(ent, day0, day0.AddDays(30).AddSeconds(-1), true).Show);
        Assert.True(ProNotice.Decide(ent, day0, day0.AddDays(30), true).Show);
    }

    [Fact]
    public void Bat_buoc_4_co_Pro_khong_hien()
    {
        using var kit = new ProTestKit();
        var d = ProNotice.Decide(kit.Ent(devices: 5, proUntil: Now.AddDays(1).ToUnixTimeSeconds()), null, Now, true);
        Assert.False(d.Show);
        Assert.Equal(ProNoticeReason.HasPro, d.Reason);
    }

    [Fact]
    public void Bat_buoc_5_ngoai_tuyen_JWS_dem_het_han_khong_hien_khong_loi()
    {
        using var kit = new ProTestKit();
        // JWS dem tu 8 ngay truoc, khong lam moi duoc (ngoai tuyen) ⇒ qua exp.
        var stale = kit.Sign(ProTestKit.Claims(devices: 3, iat: Now.AddDays(-8)));
        var src = new CachedEntitlementSource(stale, kit.Verifier());
        using var dir = new TempDir();
        var output = new StringWriter();
        var prompt = new ProNoticePrompt(src, new LocalStateStore(dir.Path), ProTestKit.ClientId, output,
                                         () => throw new InvalidOperationException("khong duoc hoi"), _ => { });

        Assert.Equal(ProNoticeChoice.NotShown, prompt.Run(Now, interactive: true, "C:\\bao-cao"));
        Assert.Equal("", output.ToString());
        Assert.Equal(ProNoticeReason.NoEntitlement, ProNotice.Decide(src.Current(Now), null, Now, true).Reason);
    }

    // ---- ADR-0002 muc 2.4 ----

    [Theory]
    [InlineData("--silent")]
    [InlineData("--json-only")]
    public void Khong_hien_o_silent_va_json_only(string flag)
    {
        using var kit = new ProTestKit();
        var opts = CliOptions.Parse([flag]);
        using var dir = new TempDir();
        var output = new StringWriter();
        var prompt = new ProNoticePrompt(new FakeEntitlementSource(kit.Ent(devices: 9)), new LocalStateStore(dir.Path),
                                         ProTestKit.ClientId, output,
                                         () => throw new InvalidOperationException("khong duoc cho phim"), _ => { });

        Assert.Equal(ProNoticeChoice.NotShown, prompt.Run(Now, interactive: !opts.Silent, "x"));
        Assert.Equal("", output.ToString());
        Assert.False(File.Exists(Path.Combine(dir.Path, LocalStateStore.FileName)));   // khong de dau vet
    }

    [Fact]
    public void Chon_1_mo_trang_Pro_va_ghi_moc()
    {
        using var kit = new ProTestKit();
        using var dir = new TempDir();
        var state = new LocalStateStore(dir.Path);
        string? opened = null;
        var prompt = new ProNoticePrompt(new FakeEntitlementSource(kit.Ent(devices: 2)), state, "tsuswico",
                                         new StringWriter(), () => "1", u => opened = u);

        Assert.Equal(ProNoticeChoice.SeePlans, prompt.Run(Now, true, "x"));
        Assert.Equal("https://tsudev.com/pro?app=tsuswico", opened);
        Assert.Equal(Now, state.GetTimestamp(ProNotice.LastShownKey));
        // Lan chay ke tiep trong 30 ngay: khong hien nua.
        Assert.Equal(ProNoticeChoice.NotShown, prompt.Run(Now.AddDays(1), true, "x"));
    }

    [Theory]
    [InlineData("2")]
    [InlineData("")]
    [InlineData(null)]          // het dau vao (stdin bi dong)
    [InlineData("abc")]
    public void Moi_phim_khac_la_De_sau_va_van_ghi_moc(string? answer)
    {
        using var kit = new ProTestKit();
        using var dir = new TempDir();
        var state = new LocalStateStore(dir.Path);
        var opened = false;
        var prompt = new ProNoticePrompt(new FakeEntitlementSource(kit.Ent(devices: 2)), state, "c",
                                         new StringWriter(), () => answer, _ => opened = true);

        Assert.Equal(ProNoticeChoice.NotNow, prompt.Run(Now, true, "x"));
        Assert.False(opened);
        Assert.NotNull(state.GetTimestamp(ProNotice.LastShownKey));
    }

    [Fact]
    public void Moc_luu_UTC_ISO_8601_va_giu_khoa_khac()
    {
        using var dir = new TempDir();
        Directory.CreateDirectory(dir.Path);
        var file = Path.Combine(dir.Path, LocalStateStore.FileName);
        File.WriteAllText(file, "{\"khac\":42}");
        var state = new LocalStateStore(dir.Path);
        state.SetTimestamp(ProNotice.LastShownKey, new DateTimeOffset(2026, 11, 20, 15, 30, 0, TimeSpan.FromHours(7)));

        var json = File.ReadAllText(file);
        Assert.Contains("\"tsu.pro_notice.last_shown\": \"2026-11-20T08:30:00Z\"", json);
        Assert.Contains("\"khac\": 42", json);
    }

    [Fact]
    public void Tep_state_hong_coi_nhu_rong()
    {
        using var dir = new TempDir();
        Directory.CreateDirectory(dir.Path);
        File.WriteAllText(Path.Combine(dir.Path, LocalStateStore.FileName), "{hong");
        Assert.Null(new LocalStateStore(dir.Path).GetTimestamp(ProNotice.LastShownKey));
    }

    [Fact]
    public void Noi_dung_nguyen_van_tieng_Viet()
    {
        using var _ = Loc.Use(AppLanguage.Vi);
        Assert.Equal("Dữ liệu chỉ lưu trên thiết bị này", ProNotice.Title);
        Assert.Equal("Tài khoản của bạn đang dùng trên 3 thiết bị. Ở bản miễn phí, dữ liệu chỉ lưu trên từng thiết bị, không gửi lên internet hay đám mây, nên các thiết bị không tự đồng bộ với nhau. Để đồng bộ và sao lưu dữ liệu giữa các thiết bị, hãy nâng cấp lên TsuDEV Pro.", ProNotice.Body(3));
        Assert.Equal("Xem gói Pro", ProNotice.PrimaryButton);
        Assert.Equal("Để sau", ProNotice.SecondaryButton);
        Assert.Equal("Sao lưu ra tệp (miễn phí)", ProNotice.BackupLink);
    }

    [Fact]
    public void Noi_dung_nguyen_van_tieng_Anh()
    {
        using var _ = Loc.Use(AppLanguage.En);
        Assert.Equal("Your data stays on this device", ProNotice.Title);
        Assert.Equal("Your account is signed in on 3 devices. On the free plan, data is stored only on each device and is never sent to the internet or the cloud, so your devices do not sync with each other. To sync and back up your data across devices, upgrade to TsuDEV Pro.", ProNotice.Body(3));
        Assert.Equal("See Pro plans", ProNotice.PrimaryButton);
        Assert.Equal("Not now", ProNotice.SecondaryButton);
        Assert.Equal("Back up to a file (free)", ProNotice.BackupLink);
    }

    [Fact]
    public void Noi_dung_khop_bang_trong_PRODUCT_POLICY()
    {
        // Doi chieu voi chinh tai lieu quy uoc: ai sua mot ben se thay test do.
        var policy = File.ReadAllText(Path.Combine(RepoRoot(), ".standards", "docs", "PRODUCT_POLICY.md"));
        foreach (var lang in new[] { AppLanguage.Vi, AppLanguage.En })
        {
            using var _ = Loc.Use(lang);
            foreach (var s in new[] { ProNotice.Title, ProNotice.Body(9).Replace("9", "{n}", StringComparison.Ordinal),
                                      ProNotice.PrimaryButton, ProNotice.SecondaryButton, ProNotice.BackupLink })
                Assert.Contains($"| {s} |", policy);
        }
    }

    private static string RepoRoot()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "Tsudev.SystemAudit.sln"))) return d.FullName;
        throw new InvalidOperationException("Khong tim thay goc repo.");
    }
}
