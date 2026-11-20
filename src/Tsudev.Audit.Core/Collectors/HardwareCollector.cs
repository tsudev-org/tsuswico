using Tsudev.Audit.Core.Localization;
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
        var t = DataTable.Create("", Loc.T("Tên máy", "Computer name"), Loc.T("Loại thiết bị", "Device type"), Loc.T("Hãng sản xuất", "Manufacturer"), "Model",
            Loc.T("Số Serial (Chassis)", "Serial number (Chassis)"), "BIOS Version", Loc.T("Mainboard - Model", "Motherboard - Model"), Loc.T("Tổng RAM", "Total RAM"))
            .Numeric(Loc.T("Tổng RAM", "Total RAM"));

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
        _ => Loc.T("Không xác định", "Unknown")
    };

    public static DataTable CollectCpu(SystemContext ctx)
    {
        var t = DataTable.Create("", Loc.T("Tên CPU", "CPU name"), Loc.T("Hãng sản xuất", "Manufacturer"), Loc.T("Số nhân (Core)", "Cores"),
            Loc.T("Số luồng (Thread)", "Threads"), Loc.T("Xung nhịp tối đa (MHz)", "Max clock (MHz)"), "Socket")
            .Numeric(Loc.T("Số nhân (Core)", "Cores"), Loc.T("Số luồng (Thread)", "Threads"), Loc.T("Xung nhịp tối đa (MHz)", "Max clock (MHz)"));
        foreach (var c in ctx.Wmi.Query("Win32_Processor"))
            t.AddRow(c.Str("Name"), c.Str("Manufacturer"), c.Num("NumberOfCores"),
                c.Num("NumberOfLogicalProcessors"), c.Num("MaxClockSpeed"), c.Str("SocketDesignation"));
        t.AddEmptyNotice(Loc.T("Không đọc được thông tin CPU", "Could not read CPU information"));
        return t;
    }

    public static (DataTable Table, int Used, string Total) CollectRam(SystemContext ctx)
    {
        var t = DataTable.Create("", Loc.T("Khe cắm (Slot)", "Slot"), Loc.T("Dung lượng", "Capacity"), Loc.T("Tốc độ (MHz)", "Speed (MHz)"),
            Loc.T("Loại RAM", "RAM type"), Loc.T("Hãng sản xuất", "Manufacturer"), "Serial Number")
            .Numeric(Loc.T("Dung lượng", "Capacity"), Loc.T("Tốc độ (MHz)", "Speed (MHz)"));
        var sticks = ctx.Wmi.Query("Win32_PhysicalMemory");
        foreach (var m in sticks)
            t.AddRow(m.Str("DeviceLocator"), FormatBytes(m.Num("Capacity")), m.Num("Speed"),
                DescribeMemoryType(m.Num("SMBIOSMemoryType") ?? m.Num("MemoryType")),
                m.Str("Manufacturer"), m.Str("SerialNumber"));
        t.AddEmptyNotice(Loc.T("Không đọc được thông tin RAM", "Could not read RAM information"));

        var array = ctx.Wmi.Query("Win32_PhysicalMemoryArray").FirstRowOrNull();
        var totalSlots = array?.Num("MemoryDevices")?.ToString(CultureInfo.InvariantCulture) ?? "-";
        return (t, sticks.Count, totalSlots);
    }

    public static string DescribeMemoryType(long? code) => code switch
    {
        20 => "DDR", 21 => "DDR2", 24 => "DDR3", 26 => "DDR4", 34 => "DDR5",
        _ => Loc.T("Không xác định", "Unknown")
    };

    public static DataTable CollectDisks(SystemContext ctx)
    {
        var t = DataTable.Create("", Loc.T("Tên / Model", "Name / Model"), Loc.T("Chuẩn giao tiếp", "Interface"), Loc.T("Dung lượng", "Capacity"), "Serial Number", "Firmware")
            .Numeric(Loc.T("Dung lượng", "Capacity"));
        foreach (var d in ctx.Wmi.Query("Win32_DiskDrive"))
            t.AddRow(d.Str("Model"), d.Str("InterfaceType"), FormatBytes(d.Num("Size")),
                d.Str("SerialNumber"), d.Str("FirmwareRevision"));
        t.AddEmptyNotice(Loc.T("Không đọc được thông tin ổ đĩa", "Could not read disk information"));
        return t;
    }

    public static DataTable CollectVolumes(SystemContext ctx)
    {
        var t = DataTable.Create("", Loc.T("Ổ đĩa", "Drive"), Loc.T("Nhãn", "Label"), Loc.T("Hệ thống file", "File system"),
            Loc.T("Tổng dung lượng", "Total size"), Loc.T("Dung lượng trống", "Free space"), Loc.T("Tỷ lệ còn trống (%)", "Free (%)"))
            .Numeric(Loc.T("Tổng dung lượng", "Total size"), Loc.T("Dung lượng trống", "Free space"), Loc.T("Tỷ lệ còn trống (%)", "Free (%)"));
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
        t.AddEmptyNotice(Loc.T("Không đọc được phân vùng nào", "No volumes could be read"));
        return t;
    }

    public static DataTable CollectGpu(SystemContext ctx)
    {
        var t = DataTable.Create("", Loc.T("Tên GPU", "GPU name"), Loc.T("VRAM (ước tính)", "VRAM (estimated)"), Loc.T("Độ phân giải hiện tại", "Current resolution"), Loc.T("Phiên bản Driver", "Driver version"))
            .Numeric(Loc.T("VRAM (ước tính)", "VRAM (estimated)"));
        foreach (var g in ctx.Wmi.Query("Win32_VideoController"))
        {
            var res = g.Num("CurrentHorizontalResolution") is { } w && g.Num("CurrentVerticalResolution") is { } h
                ? $"{w} x {h}" : "-";
            t.AddRow(g.Str("Name"), FormatBytes(g.Num("AdapterRAM")), res, g.Str("DriverVersion"));
        }
        t.AddEmptyNotice(Loc.T("Không đọc được card đồ họa", "Could not read the graphics card"));
        return t;
    }

    public static DataTable CollectNetwork(SystemContext ctx)
    {
        var t = DataTable.Create("", Loc.T("Tên adapter", "Adapter name"), Loc.T("Địa chỉ MAC", "MAC address"), Loc.T("Địa chỉ IP", "IP address"), "Default Gateway", "DHCP");
        t.Searchable = true;
        foreach (var n in ctx.Wmi.Query("Win32_NetworkAdapterConfiguration", "IPEnabled=TRUE"))
        {
            t.AddRow(n.Str("Description"), n.Str("MACAddress"),
                JoinArray(n, "IPAddress"), JoinArray(n, "DefaultIPGateway"),
                n.Bool("DHCPEnabled") == true ? Loc.T("Có", "Yes") : Loc.T("Không", "No"));
        }
        t.AddEmptyNotice(Loc.T("Không có card mạng nào đang hoạt động", "No active network adapters"));
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
        var t = DataTable.Create("", Loc.T("Tên thiết bị", "Device name"), Loc.T("Mã lỗi", "Error code"), Loc.T("Ý nghĩa", "Meaning"), Loc.T("Nhà sản xuất", "Manufacturer"))
            .Numeric(Loc.T("Mã lỗi", "Error code"));
        foreach (var d in ctx.Wmi.Query("Win32_PNPEntity", "ConfigManagerErrorCode <> 0"))
        {
            var code = d.Num("ConfigManagerErrorCode");
            t.AddRow(d.Str("Name"), code, DescribeDeviceError(code), d.Str("Manufacturer"));
        }
        t.AddEmptyNotice(Loc.T("Không phát hiện thiết bị nào bị lỗi driver", "No devices with driver errors found"));
        return t;
    }

    public static string DescribeDeviceError(long? code) => code switch
    {
        1 => Loc.T("Thiết bị chưa được cấu hình đúng", "Device is not configured correctly"),
        3 => Loc.T("Driver bị lỗi hoặc thiếu tài nguyên", "Driver error or insufficient resources"),
        10 => Loc.T("Thiết bị không khởi động được", "Device cannot start"),
        18 => Loc.T("Cần cài lại driver", "Driver needs reinstalling"),
        19 => Loc.T("Registry bị lỗi", "Registry error"),
        22 => Loc.T("Thiết bị đã bị vô hiệu hóa (Disabled)", "Device is disabled"),
        24 => Loc.T("Thiết bị không hiện diện/lỗi/thiếu driver", "Device not present, failing or missing a driver"),
        28 => Loc.T("Chưa cài driver cho thiết bị này", "No driver installed for this device"),
        31 => Loc.T("Windows không thể tải driver", "Windows cannot load the driver"),
        37 => Loc.T("Windows không thể khởi tạo driver", "Windows cannot initialize the driver"),
        39 => Loc.T("Driver bị hỏng hoặc thiếu", "Driver is corrupt or missing"),
        43 => Loc.T("Windows đã dừng thiết bị do lỗi", "Windows stopped the device because of a problem"),
        45 => Loc.T("Thiết bị hiện không kết nối", "Device is not currently connected"),
        _ => Loc.T("Mã lỗi khác - xem Device Manager", "Other error code - see Device Manager")
    };
}
