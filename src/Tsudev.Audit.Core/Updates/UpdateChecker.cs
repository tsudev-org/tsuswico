using Tsudev.Audit.Core.Localization;
using Tsudev.Audit.Core.Abstractions;

namespace Tsudev.Audit.Core.Updates;

/// <summary>
/// Quyet dinh xem co phai cap nhat truoc khi quet hay khong.
///
/// QUYET DINH THIET KE QUAN TRONG NHAT - kiem tra that bai thi KHONG chan:
///
/// Cong cu nay duoc dung dung o nhung noi mang bi han che nhat - may trong
/// mang noi bo cach ly, may trong doanh nghiep chan GitHub, may khong noi
/// mang. Neu "khong kiem tra duoc" cung chan luon lan quet, cong cu se vo dung
/// chinh o noi no can thiet nhat, va nguoi dung khong co cach nao vuot qua.
///
/// Vi vay chi chan khi da XAC DINH CHAC CHAN co ban moi hon. Moi truong hop
/// khong chac chan deu cho di tiep, kem mot ghi chu trong bao cao de nguoi doc
/// biet lan quet nay chua doi chieu duoc phien ban.
/// </summary>
public sealed class UpdateChecker
{
    private readonly IUpdateFeed _feed;

    public UpdateChecker(IUpdateFeed feed)
        => _feed = feed ?? throw new ArgumentNullException(nameof(feed));

    /// <param name="currentVersionText">So hieu cua ban dang chay.</param>
    /// <param name="kind">
    /// Ban dang chay den tu dau. Ban portable KHONG tu cai duoc: chay file
    /// setup se cai mot ban thu hai vao Program Files trong khi file tren USB
    /// van cu, va lan sau chay lai no thi lai bi chan tiep.
    /// </param>
    public UpdateCheckResult Check(string currentVersionText, InstallKind kind = InstallKind.Installed)
    {
        if (!VersionNumber.TryParse(currentVersionText, out var current))
        {
            // Khong doc duoc phien ban cua CHINH minh thi khong co co so so sanh.
            return new UpdateCheckResult(UpdateStatus.CheckFailed, VersionNumber.None,
                Message: Loc.T($"Không đọc được phiên bản hiện tại ('{currentVersionText}'). Bỏ qua kiểm tra cập nhật.", $"Could not read the current version ('{currentVersionText}'). Skipping the update check."));
        }

        ReleaseInfo? latest;
        string? error;
        try
        {
            latest = _feed.GetLatest(out error);
        }
        catch (Exception ex)   // cong cu KHONG duoc sap vi mot loi mang
        {
            return new UpdateCheckResult(UpdateStatus.CheckFailed, current,
                Message: Loc.T($"Không kiểm tra được bản cập nhật: {ex.Message}", $"Could not check for updates: {ex.Message}"));
        }

        if (latest is null)
        {
            return new UpdateCheckResult(UpdateStatus.CheckFailed, current,
                Message: error ?? Loc.T("Không lấy được thông tin bản phát hành mới nhất.", "Could not get information about the latest release."));
        }

        if (!latest.Version.IsValid)
        {
            return new UpdateCheckResult(UpdateStatus.CheckFailed, current, latest,
                Loc.T($"Không đọc được số hiệu phiên bản từ bản phát hành '{latest.TagName}'.", $"Could not read a version number from release '{latest.TagName}'."));
        }

        if (latest.Version <= current)
        {
            return new UpdateCheckResult(UpdateStatus.UpToDate, current, latest,
                Loc.T($"Đang dùng bản mới nhất ({current}).", $"You are using the latest version ({current})."));
        }

        // BAN PORTABLE: van chan, nhung KHONG tu cai.
        //
        // Chay file setup o day la mot cai bay: no cai mot ban thu hai vao
        // Program Files, con file .exe tren USB thi VAN CU - lan sau chay lai
        // file do se lai bi chan, mai mai. Nguoi dung khong co cach nao thoat
        // ra ma khong hieu chuyen gi dang xay ra.
        if (kind == InstallKind.Portable)
        {
            var where = string.IsNullOrWhiteSpace(latest.PortableUrl) ? latest.PageUrl : latest.PortableUrl;
            return new UpdateCheckResult(UpdateStatus.UpdateRequired, current, latest,
                Loc.T($"Đã có phiên bản mới {latest.Version} (bạn đang dùng {current}). " +
                      $"Đây là bản portable nên phải tự thay thế: tải {where}, " +
                      "giải nén rồi ghi đè lên thư mục đang chạy.",
                      $"Version {latest.Version} is available (you are using {current}). " +
                      $"This is the portable edition, so replace it yourself: download {where}, " +
                      "then extract it over the folder it runs from."),
                CanSelfInstall: false);
        }

        // Co ban moi nhung KHONG co file cai dat -> khong the cap nhat tu dong.
        // Bao cho nguoi dung biet nhung khong chan ho lai.
        if (string.IsNullOrWhiteSpace(latest.InstallerUrl))
        {
            return new UpdateCheckResult(UpdateStatus.CheckFailed, current, latest,
                Loc.T($"Có bản mới {latest.Version} nhưng bản phát hành đó không kèm file cài đặt. " +
                      $"Vui lòng cập nhật thủ công tại {latest.PageUrl}",
                      $"Version {latest.Version} is available but that release has no installer. " +
                      $"Please update manually from {latest.PageUrl}"));
        }

        return new UpdateCheckResult(UpdateStatus.UpdateRequired, current, latest,
            Loc.T($"Đã có phiên bản mới {latest.Version} (bạn đang dùng {current}).",
                  $"Version {latest.Version} is available (you are using {current})."),
            CanSelfInstall: true);
    }
}
