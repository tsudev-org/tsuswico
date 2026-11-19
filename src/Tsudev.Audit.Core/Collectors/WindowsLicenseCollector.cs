using Tsudev.Audit.Core.Localization;
using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>Trang thai kich hoat ban quyen Windows (SoftwareLicensingProduct).</summary>
public static class WindowsLicenseCollector
{
    /// <summary>Ma trang thai license cua Windows -> mo ta tieng Viet.</summary>
    public static string DescribeStatus(long? code) => code switch
    {
        0 => Loc.T("Unlicensed (Chưa kích hoạt)", "Unlicensed (Not activated)"),
        1 => Loc.T("Licensed (Đã kích hoạt)", "Licensed (Activated)"),
        2 => Loc.T("OOB Grace (Đang trong thời gian dùng thử)", "OOB Grace (In trial period)"),
        3 => Loc.T("OOT Grace (Hết hạn dùng thử)", "OOT Grace (Trial expired)"),
        4 => Loc.T("Non-Genuine Grace (Nghi ngờ không hợp lệ)", "Non-Genuine Grace (Suspected non-genuine)"),
        5 => Loc.T("Notification (Chưa kích hoạt - đã hết hạn thông báo)", "Notification (Not activated - notification period expired)"),
        6 => Loc.T("Extended Grace (Gia hạn mở rộng)", "Extended Grace (Extended grace period)"),
        _ => Loc.T("Không xác định", "Unknown")
    };

    /// <summary>
    /// Xep ma trang thai license vao mot trong ba nhom de ket luan.
    /// Phan nhom nay quyet dinh ket luan cuoi cung nen phai ro rang.
    /// </summary>
    public static LicenseHealth Classify(long? code) => code switch
    {
        1 => LicenseHealth.Ok,                                  // Licensed

        // Ba trang thai duoi day deu nghia la KHONG kich hoat hop le.
        0 or 4 or 5 => LicenseHealth.Problem,                   // Unlicensed / Non-Genuine Grace / Notification

        // Con han dung thu: chua sai, nhung cung CHUA phai hop le.
        2 or 3 or 6 => LicenseHealth.Grace,

        _ => LicenseHealth.Unknown
    };

    public static (DataTable Table, WindowsLicenseSummary Summary) Collect(SystemContext ctx)
    {
        var t = DataTable.Create("", Loc.T("Sản phẩm", "Product"), Loc.T("Mô tả", "Description"), Loc.T("Trạng thái", "Status"),
            Loc.T("Product Key (5 ký tự cuối)", "Product Key (last 5 characters)"), Loc.T("Kênh license", "License channel"));

        // Chi lay san pham co PartialProductKey (tuc la co license thuc su gan
        // vao may), tranh liet ke hang chuc SKU rong ma Windows luon khai bao.
        var rows = ctx.Wmi.Query("SoftwareLicensingProduct",
            "PartialProductKey IS NOT NULL AND ApplicationID='55c92734-d682-4d71-983e-d6ec3f16059f'");

        if (rows.Count == 0)
            ctx.Warn(Loc.T("Không đọc được SoftwareLicensingProduct (cần quyền Administrator để thấy đầy đủ).", "Could not read SoftwareLicensingProduct (Administrator rights are needed for full data)."));

        int ok = 0, problem = 0, grace = 0, unknown = 0;
        foreach (var r in rows)
        {
            var status = r.Num("LicenseStatus");
            switch (Classify(status))
            {
                case LicenseHealth.Ok: ok++; break;
                case LicenseHealth.Problem: problem++; break;
                case LicenseHealth.Grace: grace++; break;
                default: unknown++; break;
            }

            t.AddRow(
                r.Str("Name"),
                r.Str("Description"),
                DescribeStatus(status),
                r.Str("PartialProductKey"),
                r.Str("ProductKeyChannel"));
        }

        t.AddEmptyNotice(Loc.T("Không tìm thấy sản phẩm Windows nào có license gắn vào máy", "No Windows product with a license attached to this machine was found"));
        return (t, new WindowsLicenseSummary(rows.Count, ok, problem, grace, unknown));
    }
}

/// <summary>Nhom trang thai license, dung de ket luan.</summary>
public enum LicenseHealth { Unknown, Ok, Grace, Problem }
