namespace NetAddressWinUI.Models;

public class HardwareInfo
{
    public CpuInfo cpu { get; set; } = new();
    public MemoryInfo memory { get; set; } = new();
    public StorageInfo storage { get; set; } = new();
    public GpuInfo gpu { get; set; } = new();
    public NetworkInfo network { get; set; } = new();
    public OsInfo os { get; set; } = new();
    public MotherboardInfo motherboard { get; set; } = new();
}

public class CpuInfo
{
    public string model { get; set; } = string.Empty;
    public int cores { get; set; }
    public double speed { get; set; }
    public double usage { get; set; }
}

public class MemoryInfo
{
    public double total { get; set; }
    public double used { get; set; }
    public double available { get; set; }
    public double usage { get; set; }
}

public class StorageInfo
{
    public double total { get; set; }
    public double used { get; set; }
    public double available { get; set; }
    public double usage { get; set; }
}

public class GpuInfo
{
    public string model { get; set; } = string.Empty;
    public double memory { get; set; }
    public double usage { get; set; }
}

public class NetworkInfo
{
    public List<NetworkInterface> interfaces { get; set; } = new();
}

public class NetworkInterface
{
    public string name { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public int speed { get; set; }
}

public class OsInfo
{
    public string name { get; set; } = string.Empty;
    public string version { get; set; } = string.Empty;
    public string architecture { get; set; } = string.Empty;
    public long uptime { get; set; }
}

public class MotherboardInfo
{
    public string manufacturer { get; set; } = string.Empty;
    public string model { get; set; } = string.Empty;
}

public class RegisterMessage
{
    public string messageType { get; set; } = "register";
    public string userId { get; set; } = string.Empty;
    public string macAddress { get; set; } = string.Empty;
    public string ipAddress { get; set; } = string.Empty;
    public string machineName { get; set; } = string.Empty;
    public HardwareInfo hardware { get; set; } = new();
}

public class DisconnectMessage
{
    public string messageType { get; set; } = "disconnect";
    public string macAddress { get; set; } = string.Empty;
}

public class ShutdownMessage
{
    public string messageType { get; set; } = "shutdown";
    public string macAddress { get; set; } = string.Empty;
}

public class UpdateMessage
{
    public string messageType { get; set; } = "update";
    public string macAddress { get; set; } = string.Empty;
    public string ipAddress { get; set; } = string.Empty;
    public string machineName { get; set; } = string.Empty;
    public HardwareInfo hardware { get; set; } = new();
} 