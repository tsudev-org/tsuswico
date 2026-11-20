using Tsudev.Audit.Core.Pro;

namespace Tsudev.Audit.Tests;

/// <summary>Ma cai dat - AUTH_AND_ACCOUNT 14.3 / muc 16 ca 15, ADR-0002 muc 2.3.</summary>
public class InstallationIdTests
{
    [Fact]
    public void Du_128_bit_base64url_22_ky_tu_va_khong_trung()
    {
        var ids = Enumerable.Range(0, 200).Select(_ => InstallationId.New()).ToList();
        Assert.All(ids, id =>
        {
            Assert.Equal(22, id.Length);
            Assert.Matches("^[A-Za-z0-9_-]{22}$", id);
            Assert.Equal(16, Base64Url.TryDecode(id)!.Length);
            Assert.True(InstallationId.IsValid(id));
        });
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAA")]       // 21 ky tu
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAA")]     // 23 ky tu
    [InlineData("AAAAAAAAAAAAAAAAAAAAA+")]      // base64 thuong, khong phai url
    [InlineData("AAAAAAAAAAAAAAAAAAAAAB")]      // bit thua o ky tu cuoi
    public void Sai_dinh_dang_khong_hop_le(string? value) => Assert.False(InstallationId.IsValid(value));

    [Fact]
    public void Chi_doc_thi_khong_sinh_ma_khong_tao_tep()
    {
        using var dir = new TempDir();
        var store = new InstallationIdStore(dir.Path);
        Assert.Null(store.TryGet());
        Assert.False(Directory.Exists(dir.Path));   // ban mien phi chua dang nhap: khong dau vet
    }

    [Fact]
    public void Sinh_mot_lan_roi_giu_nguyen_xoa_thi_sinh_moi()
    {
        using var dir = new TempDir();
        var store = new InstallationIdStore(dir.Path);
        var first = store.GetOrCreate();
        Assert.Equal(first, store.GetOrCreate());
        Assert.Equal(first, new InstallationIdStore(dir.Path).TryGet());

        store.Delete();
        Assert.Null(store.TryGet());
        Assert.NotEqual(first, store.GetOrCreate());
    }

    [Fact]
    public void Tep_hong_thi_sinh_lai()
    {
        using var dir = new TempDir();
        Directory.CreateDirectory(dir.Path);
        File.WriteAllText(Path.Combine(dir.Path, InstallationIdStore.FileName), "00:11:22:33:44:55");
        var store = new InstallationIdStore(dir.Path);
        Assert.Null(store.TryGet());
        Assert.True(InstallationId.IsValid(store.GetOrCreate()));
    }

    [Fact]
    public void Ma_nguon_khong_dung_dinh_danh_phan_cung()
    {
        // Lop ma cai dat KHONG duoc cham toi bo thu thap (MAC, serial, ma san pham).
        var src = File.ReadAllText(Path.Combine(Root(), "src", "Tsudev.Audit.Core", "Pro", "InstallationId.cs"));
        foreach (var bad in new[] { "Collectors", "Serial", "MacAddress", "NetworkInterface", "ProductId", "Guid.NewGuid" })
            Assert.DoesNotContain(bad, src, StringComparison.Ordinal);
        Assert.Contains("RandomNumberGenerator.GetBytes", src, StringComparison.Ordinal);
    }

    private static string Root()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "Tsudev.SystemAudit.sln"))) return d.FullName;
        throw new InvalidOperationException();
    }
}
