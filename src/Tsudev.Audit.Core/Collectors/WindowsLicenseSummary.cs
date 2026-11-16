using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>
/// Tong hop trang thai license Windows tren may.
///
/// LUU Y VE MOT LOI DA TUNG MAC: ban dau ket luan duoc suy ra bang
/// <c>licensedCount &gt; 0</c> - tuc chi can MOT SKU bat ky o trang thai
/// Licensed la ket luan "hop le". Windows khai bao NHIEU SKU duoi cung mot
/// ApplicationID, nen mot may co SKU chinh dang Notification/Unlicensed nhung
/// co mot SKU phu Licensed van bi cham diem "hop le".
///
/// Voi cong cu nay, bao NHAM LA HOP LE la kieu sai te nhat: bao cao co the bi
/// mang ra lam can cu trong tranh chap lao dong hoac thanh tra. Vi vay quy tac
/// nay co chu dich THAN TRONG - chi ket luan hop le khi KHONG con SKU nao o
/// trang thai co van de.
/// </summary>
public sealed record WindowsLicenseSummary(
    int Total, int Ok, int Problem, int Grace, int Unknown)
{
    /// <summary>Co du lieu de ket luan hay khong.</summary>
    public bool IsKnown => Total > 0;

    /// <summary>
    /// Chi hop le khi co it nhat mot SKU Licensed VA khong con SKU nao o
    /// trang thai co van de hoac dang trong thoi gian dung thu.
    /// </summary>
    public bool IsGenuine => Total > 0 && Ok > 0 && Problem == 0 && Grace == 0;

    /// <summary>Co it nhat mot SKU o trang thai chac chan khong hop le.</summary>
    public bool HasProblem => Problem > 0;

    public LicenseHealth Overall
        => !IsKnown ? LicenseHealth.Unknown
         : HasProblem ? LicenseHealth.Problem
         : Grace > 0 || Unknown > 0 ? LicenseHealth.Grace
         : Ok > 0 ? LicenseHealth.Ok
         : LicenseHealth.Unknown;

    public string Describe()
        => $"{Total} sản phẩm: {Ok} hợp lệ, {Problem} có vấn đề, {Grace} đang dùng thử, {Unknown} không xác định";
}
