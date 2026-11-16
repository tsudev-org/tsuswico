using System.Globalization;
using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>Thu thap cau hinh phan cung qua WMI.</summary>
public static class HardwareCollector
{
    public static string FormatBytes(long? bytes)
    {
        if (bytes is null or <= 0) return "-";
        string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };
        double v = bytes.Value;
        int i = 0;
        while (v >= 1024 && i < units.Length - 1) { v /= 1024; i++; }
        return $"{v:N1} {units[i]}";
    }

    public static (DataTable Table, string Summary) CollectOverview(SystemContext ctx)
    {
        var t = DataTable.Create("", "Tên máy", "Loại thiết bị", "Hãng sản xuất", "Model",
            "Số Serial (Chassis)", "BIOS Version", "Mainboard - Model", "Tổng RAM")
            .Numeric("Tổng RAM");

        var cs = ctx.Wmi.Query("Win32_ComputerSystem").FirstRowOrNull();
        var bios = ctx.Wmi.Query("Win32_BIOS").FirstRowOrNull();
        var board = ctx.Wmi.Query("Win32_BaseBoard").FirstRowOrNull();
        var chassis = ctx.Wmi.Query("Win32_SystemEnclosure").FirstRowOrNull();

        var ram = FormatBytes(cs?.Num("TotalPhysicalMemory"));
        var deviceType = DescribeChassis(chassis?.Num("ChassisTypes"));
        var manufacturer = cs?.Str("Manufacturer") ?? "-";
        var model = cs?.Str("Model") ?? "-";

        t.AddRow(ctx.ComputerName, deviceType, manufacturer, model,
            bios?.Str("SerialNumber") ?? "-", bios?.Str("SMBIOSBIOSVersion") ?? "-",
            board?.Str("Product") ?? "-", ram);

        var cpuCount = ctx.Wmi.Query("Win32_Processor").Count;
        var summary = $"{manufacturer} {model} · {cpuCount} CPU · {ram}";
        return (t, summary);
    }

    /// <summary>Ma ChassisTypes cua SMBIOS -> loai thiet bi.</summary>
    public static string DescribeChassis(long? code) => code switch
    {
        3 or 4 or 6 or 7 or 15 => "Desktop",
        8 or 9 or 10 or 14 => "Laptop",
        11 or 12 => "Handheld/Notebook",
        17 or 23 or 28 => "Server",
        30 or 31 or 32 => "Tablet",
        _ => "Không xác định"
    };

    public static DataTable CollectCpu(SystemContext ctx)
    {
        var t = DataTable.Create("", "Tên CPU", "Hãng sản xuất", "Số nhân (Core)",
            "Số luồng (Thread)", "Xung nhịp tối đa (MHz)", "Socket")
            .Numeric("Số nhân (Core)", "Số luồng (Thread)", "Xung nhịp tối đa (MHz)");
        foreach (var c in ctx.Wmi.Query("Win32_Processor"))
            t.AddRow(c.Str("Name"), c.Str("Manufacturer"), c.Num("NumberOfCores"),
                c.Num("NumberOfLogicalProcessors"), c.Num("MaxClockSpeed"), c.Str("SocketDesignation"));
        t.AddEmptyNotice("Không đọc được thông tin CPU");
        return t;
    }

    public static (DataTable Table, int Used, string Total) CollectRam(SystemContext ctx)
    {
        var t = DataTable.Create("", "Khe cắm (Slot)", "Dung lượng", "Tốc độ (MHz)",
            "Loại RAM", "Hãng sản xuất", "Serial Number")
            .Numeric("Dung lượng", "Tốc độ (MHz)");
        var sticks = ctx.Wmi.Query("Win32_PhysicalMemory");
        foreach (var m in sticks)
            t.AddRow(m.Str("DeviceLocator"), FormatBytes(m.Num("Capacity")), m.Num("Speed"),
                DescribeMemoryType(m.Num("SMBIOSMemoryType") ?? m.Num("MemoryType")),
                m.Str("Manufacturer"), m.Str("SerialNumber"));
        t.AddEmptyNotice("Không đọc được thông tin RAM");

        var array = ctx.Wmi.Query("Win32_PhysicalMemoryArray").FirstRowOrNull();
        var totalSlots = array?.Num("MemoryDevices")?.ToString(CultureInfo.InvariantCulture) ?? "-";
        return (t, sticks.Count, totalSlots);
    }

    public static string DescribeMemoryType(long? code) => code switch
    {
        20 => "DDR", 21 => "DDR2", 24 => "DDR3", 26 => "DDR4", 34 => "DDR5",
        _ => "Không xác định"
    };

    public static DataTable CollectDisks(SystemContext ctx)
    {
        var t = DataTable.Create("", "Tên / Model", "Chuẩn giao tiếp", "Dung lượng", "Serial Number", "Firmware")
            .Numeric("Dung lượng");
        foreach (var d in ctx.Wmi.Query("Win32_DiskDrive"))
            t.AddRow(d.Str("Model"), d.Str("InterfaceType"), FormatBytes(d.Num("Size")),
                d.Str("SerialNumber"), d.Str("FirmwareRevision"));
        t.AddEmptyNotice("Không đọc được thông tin ổ đĩa");
        return t;
    }

    public static DataTable CollectVolumes(SystemContext ctx)
    {
        var t = DataTable.Create("", "Ổ đĩa", "Nhãn", "Hệ thống file",
            "Tổng dung lượng", "Dung lượng trống", "Tỷ lệ còn trống (%)")
            .Numeric("Tổng dung lượng", "Dung lượng trống", "Tỷ lệ còn trống (%)");
        t.Searchable = true;
        // DriveType=3 : o dia cuc bo (loai bo o mang/USB de tranh nhieu)
        foreach (var v in ctx.Wmi.Query("Win32_LogicalDisk", "DriveType=3"))
        {
            var size = v.Num("Size");
            var free = v.Num("FreeSpace");
            var pct = (size is > 0 && free is not null)
                ? Math.Round(free.Value * 100.0 / size.Value, 1).ToString("0.0", CultureInfo.InvariantCulture)
                : "-";
            t.AddRow(v.Str("DeviceID"), v.Str("VolumeName"), v.Str("FileSystem"),
                FormatBytes(size), FormatBytes(free), pct);
        }
        t.AddEmptyNotice("Không đọc được phân vùng nào");
        return t;
    }

    public static DataTable CollectGpu(SystemContext ctx)
    {
        var t = DataTable.Create("", "Tên GPU", "VRAM (ước tính)", "Độ phân giải hiện tại", "Phiên bản Driver")
            .Numeric("VRAM (ước tính)");
        foreach (var g in ctx.Wmi.Query("Win32_VideoController"))
        {
            var res = g.Num("CurrentHorizontalResolution") is { } w && g.Num("CurrentVerticalResolution") is { } h
                ? $"{w} x {h}" : "-";
            t.AddRow(g.Str("Name"), FormatBytes(g.Num("AdapterRAM")), res, g.Str("DriverVersion"));
        }
        t.AddEmptyNotice("Không đọc được card đồ họa");
        return t;
    }

    public static DataTable CollectNetwork(SystemContext ctx)
    {
        var t = DataTable.Create("", "Tên adapter", "Địa chỉ MAC", "Địa chỉ IP", "Default Gateway", "DHCP");
        t.Searchable = true;
        foreach (var n in ctx.Wmi.Query("Win32_NetworkAdapterConfiguration", "IPEnabled=TRUE"))
        {
            t.AddRow(n.Str("Description"), n.Str("MACAddress"),
                JoinArray(n, "IPAddress"), JoinArray(n, "DefaultIPGateway"),
                n.Bool("DHCPEnabled") == true ? "Có" : "Không");
        }
        t.AddEmptyNotice("Không có card mạng nào đang hoạt động");
        return t;
    }

    private static string JoinArray(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var v) || v is null) return "-";
        if (v is string[] arr) return arr.Length == 0 ? "-" : string.Join(", ", arr);
        if (v is System.Collections.IEnumerable e and not string)
            return string.Join(", ", e.Cast<object?>().Where(x => x is not null));
        return v.ToString() ?? "-";
    }

    /// <summary>Driver loi trong Device Manager (ConfigManagerErrorCode != 0).</summary>
    public static DataTable CollectDriverErrors(SystemContext ctx)
    {
        var t = DataTable.Create("", "Tên thiết bị", "Mã lỗi", "Ý nghĩa", "Nhà sản xuất")
            .Numeric("Mã lỗi");
        foreach (var d in ctx.Wmi.Query("Win32_PNPEntity", "ConfigManagerErrorCode <> 0"))
        {
            var code = d.Num("ConfigManagerErrorCode");
            t.AddRow(d.Str("Name"), code, DescribeDeviceError(code), d.Str("Manufacturer"));
        }
        t.AddEmptyNotice("Không phát hiện thiết bị nào bị lỗi driver");
        return t;
    }

    public static string DescribeDeviceError(long? code) => code switch
    {
        1 => "Thiết bị chưa được cấu hình đúng",
        3 => "Driver bị lỗi hoặc thiếu tài nguyên",
        10 => "Thiết bị không khởi động được",
        18 => "Cần cài lại driver",
        19 => "Registry bị lỗi",
        22 => "Thiết bị đã bị vô hiệu hóa (Disabled)",
        24 => "Thiết bị không hiện diện/lỗi/thiếu driver",
        28 => "Chưa cài driver cho thiết bị này",
        31 => "Windows không thể tải driver",
        37 => "Windows không thể khởi tạo driver",
        39 => "Driver bị hỏng hoặc thiếu",
        43 => "Windows đã dừng thiết bị do lỗi",
        45 => "Thiết bị hiện không kết nối",
        _ => "Mã lỗi khác - xem Device Manager"
    };
}
