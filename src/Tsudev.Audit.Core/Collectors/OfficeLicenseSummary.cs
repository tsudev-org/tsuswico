using Tsudev.Audit.Core.Localization;
using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>
/// Tong hop trang thai ban quyen Office.
///
/// Vi sao Office phai co tieng noi trong ket luan: mot may co Windows hop le
/// nhung Office CHUA KICH HOAT van la may co van de ve ban quyen. Truoc day
/// trang thai Office duoc thu thap va hien thi nhung KHONG he anh huong toi
/// ket luan tong the - nen bao cao ket luan "khong phat hien dau hieu" trong
/// khi Office dang o trang thai Notification.
/// </summary>
public sealed record OfficeLicenseSummary(
    int Total, int Ok, int Problem, int Grace, int Unknown)
{
    public bool IsInstalled => Total > 0;
    public bool HasProblem => Problem > 0;

    public LicenseHealth Overall
        => Total == 0 ? LicenseHealth.Unknown
         : Problem > 0 ? LicenseHealth.Problem
         : Grace > 0 ? LicenseHealth.Grace
         : Ok > 0 ? LicenseHealth.Ok
         : LicenseHealth.Unknown;

    public string Describe()
        => Total == 0
            ? Loc.T("Không phát hiện Office trên máy", "Office not detected on this machine")
            : Loc.T($"{Total} sản phẩm: {Ok} hợp lệ, {Problem} có vấn đề, {Grace} đang dùng thử, {Unknown} không xác định", $"{Total} products: {Ok} valid, {Problem} with problems, {Grace} in trial, {Unknown} unknown");
}
