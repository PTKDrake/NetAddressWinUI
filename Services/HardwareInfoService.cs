using System.Diagnostics;
using System.Management;
using Microsoft.Win32;
using NetAddressWinUI.Models;

namespace NetAddressWinUI.Services;

public interface IHardwareInfoService
{
    Task<HardwareInfo> GetHardwareInfoAsync();
}

public class HardwareInfoService : IHardwareInfoService
{
    private static PerformanceCounter? _cpuCounter;
    private static readonly object _cpuCounterLock = new object();
    private static DateTime _lastCpuCounterInit = DateTime.MinValue;
    
    // Cache for static information that doesn't change frequently
    private static CpuInfo? _cachedCpuInfo;
    private static GpuInfo? _cachedGpuInfo;
    private static MotherboardInfo? _cachedMotherboardInfo;
    private static DateTime _lastStaticInfoUpdate = DateTime.MinValue;
    private static readonly TimeSpan _staticInfoCacheTime = TimeSpan.FromMinutes(5); // Cache for 5 minutes
    
    public async Task<HardwareInfo> GetHardwareInfoAsync()
    {
        var hardwareInfo = new HardwareInfo();

        try
        {
            // Use parallel processing for faster data collection
            var tasks = new Task[]
            {
                Task.Run(() => hardwareInfo.cpu = GetCpuInfo()),
                Task.Run(() => hardwareInfo.memory = GetMemoryInfo()),
                Task.Run(() => hardwareInfo.storage = GetStorageInfo()),
                Task.Run(() => hardwareInfo.gpu = GetGpuInfo()),
                Task.Run(() => hardwareInfo.network = GetNetworkInfo()),
                Task.Run(() => hardwareInfo.os = GetOsInfo()),
                Task.Run(() => hardwareInfo.motherboard = GetMotherboardInfo())
            };

            // Wait for all tasks to complete with timeout
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error collecting hardware info: {ex.Message}");
            // Use fallback methods
            hardwareInfo.cpu = GetCpuInfoFallback();
            hardwareInfo.memory = GetMemoryInfoFallback();
            hardwareInfo.storage = GetStorageInfoFallback();
            hardwareInfo.gpu = GetGpuInfoFallback();
            hardwareInfo.network = GetNetworkInfoFallback();
            hardwareInfo.os = GetOsInfoFallback();
            hardwareInfo.motherboard = GetMotherboardInfoFallback();
        }

        return hardwareInfo;
    }

    private CpuInfo GetCpuInfo()
    {
        // Check cache first for static info
        if (_cachedCpuInfo != null && DateTime.Now - _lastStaticInfoUpdate < _staticInfoCacheTime)
        {
            // Only update dynamic info (usage)
            var cachedInfo = new CpuInfo
            {
                model = _cachedCpuInfo.model,
                cores = _cachedCpuInfo.cores,
                speed = _cachedCpuInfo.speed,
                usage = GetCpuUsage() // Always get fresh usage data
            };
            return cachedInfo;
        }

        var cpuInfo = new CpuInfo();
        
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, MaxClockSpeed FROM Win32_Processor");
            using var collection = searcher.Get();
            
            foreach (ManagementObject obj in collection)
            {
                cpuInfo.model = obj["Name"]?.ToString()?.Trim() ?? "Unknown CPU";
                cpuInfo.cores = Convert.ToInt32(obj["NumberOfCores"] ?? Environment.ProcessorCount);
                cpuInfo.speed = Convert.ToUInt32(obj["MaxClockSpeed"] ?? 0);
                break; // Get first processor
            }
            
            // Get CPU usage
            cpuInfo.usage = GetCpuUsage();
            
            // Cache static info
            _cachedCpuInfo = new CpuInfo
            {
                model = cpuInfo.model,
                cores = cpuInfo.cores,
                speed = cpuInfo.speed,
                usage = 0 // Don't cache usage
            };
            _lastStaticInfoUpdate = DateTime.Now;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting CPU info via WMI: {ex.Message}");
            return GetCpuInfoFallback();
        }

        return cpuInfo;
    }
    
    private double GetCpuUsage()
    {
        try
        {
            lock (_cpuCounterLock)
            {
                // Reinitialize counter if it's been more than 30 seconds or if it's null
                if (_cpuCounter == null || DateTime.Now - _lastCpuCounterInit > TimeSpan.FromSeconds(30))
                {
                    try
                    {
                        _cpuCounter?.Dispose();
                        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                        _lastCpuCounterInit = DateTime.Now;
                        
                        // First call to initialize
                        _cpuCounter.NextValue();
                        Thread.Sleep(50); // Reduced sleep time for faster response
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error initializing CPU counter: {ex.Message}");
                        _cpuCounter?.Dispose();
                        _cpuCounter = null;
                        return GetCpuUsageWmi();
                    }
                }

                if (_cpuCounter != null)
                {
                    var usage = _cpuCounter.NextValue();
                    
                    // Ensure usage is within valid range
                    if (usage < 0) usage = 0;
                    if (usage > 100) usage = 100;
                    
                    return Math.Round(usage, 1);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting CPU usage from performance counter: {ex.Message}");
            
            // Dispose and reset counter on error
            lock (_cpuCounterLock)
            {
                _cpuCounter?.Dispose();
                _cpuCounter = null;
            }
        }
        
        // Fallback to WMI
        return GetCpuUsageWmi();
    }
    
    private double GetCpuUsageWmi()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor");
            using var collection = searcher.Get();
            
            foreach (ManagementObject obj in collection)
            {
                var loadPercentage = obj["LoadPercentage"];
                if (loadPercentage != null)
                {
                    return Math.Round(Convert.ToDouble(loadPercentage), 1);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting CPU usage via WMI: {ex.Message}");
        }
        
        return 0;
    }

    private MemoryInfo GetMemoryInfo()
    {
        var memoryInfo = new MemoryInfo();
        
        try
        {
            // Use parallel WMI queries for faster response
            var totalMemoryTask = Task.Run(() =>
            {
                using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                using var collection = searcher.Get();
                
                foreach (ManagementObject obj in collection)
                {
                    var totalMemoryBytes = Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                    return Math.Round(totalMemoryBytes / 1024.0 / 1024.0 / 1024.0, 1);
                }
                return 0.0;
            });

            var availableMemoryTask = Task.Run(() =>
            {
                using var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
                using var collection = searcher.Get();
                
                foreach (ManagementObject obj in collection)
                {
                    var availableMemoryBytes = Convert.ToUInt64(obj["FreePhysicalMemory"]) * 1024; // KB to bytes
                    return Math.Round(availableMemoryBytes / 1024.0 / 1024.0 / 1024.0, 1);
                }
                return 0.0;
            });

            // Wait for both tasks with timeout
            Task.WaitAll(new[] { totalMemoryTask, availableMemoryTask }, TimeSpan.FromSeconds(3));

            memoryInfo.total = totalMemoryTask.Result;
            memoryInfo.available = availableMemoryTask.Result;
            memoryInfo.used = Math.Round(memoryInfo.total - memoryInfo.available, 1);
            memoryInfo.usage = memoryInfo.total > 0 ? Math.Round((memoryInfo.used / memoryInfo.total) * 100, 1) : 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting memory info via WMI: {ex.Message}");
            return GetMemoryInfoFallback();
        }

        return memoryInfo;
    }

    private StorageInfo GetStorageInfo()
    {
        var storageInfo = new StorageInfo();
        
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady);
            
            // Use parallel processing for multiple drives
            var driveInfos = drives.AsParallel().Select(drive =>
            {
                try
                {
                    var totalSize = drive.TotalSize / 1024.0 / 1024.0 / 1024.0; // Convert to GB
                    var freeSpace = drive.TotalFreeSpace / 1024.0 / 1024.0 / 1024.0; // Convert to GB
                    var usedSpace = totalSize - freeSpace;
                    return new { TotalSize = totalSize, FreeSpace = freeSpace, UsedSpace = usedSpace };
                }
                catch
                {
                    return new { TotalSize = 0.0, FreeSpace = 0.0, UsedSpace = 0.0 };
                }
            }).ToList();

            foreach (var driveInfo in driveInfos)
            {
                storageInfo.total += driveInfo.TotalSize;
                storageInfo.available += driveInfo.FreeSpace;
                storageInfo.used += driveInfo.UsedSpace;
            }
            
            storageInfo.total = Math.Round(storageInfo.total, 1);
            storageInfo.available = Math.Round(storageInfo.available, 1);
            storageInfo.used = Math.Round(storageInfo.used, 1);
            storageInfo.usage = storageInfo.total > 0 ? Math.Round((storageInfo.used / storageInfo.total) * 100, 1) : 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting storage info: {ex.Message}");
        }

        return storageInfo;
    }

    private GpuInfo GetGpuInfo()
    {
        // Check cache first
        if (_cachedGpuInfo != null && DateTime.Now - _lastStaticInfoUpdate < _staticInfoCacheTime)
        {
            // Only update dynamic info (usage)
            var cachedInfo = new GpuInfo
            {
                model = _cachedGpuInfo.model,
                memory = _cachedGpuInfo.memory,
                usage = GetGpuUsageSafe() // Always get fresh usage data
            };
            return cachedInfo;
        }

        var gpuInfo = new GpuInfo();
        
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM, PNPDeviceID FROM Win32_VideoController");
            using var collection = searcher.Get();
            
            var gpus = new List<(string name, uint memory, bool isDedicated, string pnpDeviceId)>();
            
            foreach (ManagementObject obj in collection)
            {
                var name = obj["Name"]?.ToString();
                if (string.IsNullOrEmpty(name)) continue;
                
                var adapterRam = obj["AdapterRAM"];
                uint memory = 0;
                if (adapterRam != null)
                {
                    memory = Convert.ToUInt32(adapterRam) / 1024 / 1024; // Convert to MB
                }
                
                var pnpDeviceId = obj["PNPDeviceID"]?.ToString() ?? "";
                
                // Determine if it's a dedicated GPU
                bool isDedicated = name.Contains("GeForce", StringComparison.OrdinalIgnoreCase) ||
                                 name.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ||
                                 name.Contains("RTX", StringComparison.OrdinalIgnoreCase) ||
                                 name.Contains("GTX", StringComparison.OrdinalIgnoreCase) ||
                                 name.Contains("RX ", StringComparison.OrdinalIgnoreCase);
                
                gpus.Add((name, memory, isDedicated, pnpDeviceId));
            }
            
            // Priority: Dedicated GPU first, then integrated
            var selectedGpu = gpus.FirstOrDefault(g => g.isDedicated);
            if (selectedGpu.name == null)
            {
                selectedGpu = gpus.FirstOrDefault();
            }
            
            if (selectedGpu.name != null)
            {
                gpuInfo.model = selectedGpu.name;
                gpuInfo.memory = (int)selectedGpu.memory;
                
                // Get GPU usage
                gpuInfo.usage = GetGpuUsageSafe();
                
                // Add (Integrated) suffix if it's not a dedicated GPU
                if (!selectedGpu.isDedicated && (selectedGpu.name.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
                    selectedGpu.name.Contains("AMD", StringComparison.OrdinalIgnoreCase)))
                {
                    gpuInfo.model = $"{selectedGpu.name} (Integrated)";
                }
                
                // Cache static info
                _cachedGpuInfo = new GpuInfo
                {
                    model = gpuInfo.model,
                    memory = gpuInfo.memory,
                    usage = 0 // Don't cache usage
                };
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting GPU info via WMI: {ex.Message}");
            return GetGpuInfoFallback();
        }

        return gpuInfo;
    }
    
    private double GetGpuUsageSafe()
    {
        try
        {
            // Check if GPU Engine category exists
            if (!PerformanceCounterCategory.Exists("GPU Engine"))
            {
                return 0;
            }

            var category = new PerformanceCounterCategory("GPU Engine");
            var instanceNames = category.GetInstanceNames();
            
            if (instanceNames.Length == 0)
            {
                return 0;
            }

            // Look for 3D or Graphics instances first
            var targetInstance = instanceNames.FirstOrDefault(name => 
                name.Contains("3D", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Graphics", StringComparison.OrdinalIgnoreCase)) ?? instanceNames[0];

            using var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", targetInstance, true);
            
            // Initialize counter with shorter wait time
            counter.NextValue();
            Thread.Sleep(50); // Reduced wait time for faster response
            
            var usage = counter.NextValue();
            return Math.Round(Math.Max(0, Math.Min(100, usage)), 1);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting GPU usage: {ex.Message}");
            return 0;
        }
    }

    private NetworkInfo GetNetworkInfo()
    {
        var networkInfo = new NetworkInfo();
        
        try
        {
            var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && 
                           ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback);

            foreach (var ni in interfaces)
            {
                var networkInterface = new Models.NetworkInterface
                {
                    name = ni.Name,
                    type = GetNetworkType(ni.NetworkInterfaceType),
                    speed = (int)(ni.Speed / 1000000) // Convert to Mbps
                };
                networkInfo.interfaces.Add(networkInterface);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting network info: {ex.Message}");
        }

        return networkInfo;
    }

    private string GetNetworkType(System.Net.NetworkInformation.NetworkInterfaceType type)
    {
        return type switch
        {
            System.Net.NetworkInformation.NetworkInterfaceType.Ethernet => "ethernet",
            System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211 => "wifi",
            System.Net.NetworkInformation.NetworkInterfaceType.Ppp => "ppp",
            _ => "other"
        };
    }

    private OsInfo GetOsInfo()
    {
        var osInfo = new OsInfo();
        
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem");
            using var collection = searcher.Get();
            
            foreach (ManagementObject obj in collection)
            {
                osInfo.name = obj["Caption"]?.ToString()?.Trim() ?? "Windows";
                osInfo.version = obj["Version"]?.ToString() ?? Environment.OSVersion.Version.ToString();
                osInfo.architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                osInfo.uptime = Environment.TickCount64 / 1000; // Convert to seconds
                break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting OS info via WMI: {ex.Message}");
            return GetOsInfoFallback();
        }

        return osInfo;
    }

    private MotherboardInfo GetMotherboardInfo()
    {
        // Check cache first
        if (_cachedMotherboardInfo != null && DateTime.Now - _lastStaticInfoUpdate < _staticInfoCacheTime)
        {
            return _cachedMotherboardInfo;
        }

        var motherboardInfo = new MotherboardInfo();
        
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard");
            using var collection = searcher.Get();
            
            foreach (ManagementObject obj in collection)
            {
                motherboardInfo.manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "Unknown";
                motherboardInfo.model = obj["Product"]?.ToString()?.Trim() ?? "Unknown";
                break;
            }
            
            // Cache the result
            _cachedMotherboardInfo = motherboardInfo;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting motherboard info via WMI: {ex.Message}");
            return GetMotherboardInfoFallback();
        }

        return motherboardInfo;
    }

    // Fallback methods
    private CpuInfo GetCpuInfoFallback()
    {
        var cpuInfo = new CpuInfo();
        cpuInfo.model = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown CPU";
        cpuInfo.cores = Environment.ProcessorCount;
        cpuInfo.speed = 0;
        cpuInfo.usage = 0; // Safe fallback
        return cpuInfo;
    }

    private MemoryInfo GetMemoryInfoFallback()
    {
        var memoryInfo = new MemoryInfo();
        
        try
        {
            // Try using GC for memory estimation
            var totalMemory = GC.GetTotalMemory(false);
            memoryInfo.total = Math.Round(totalMemory / 1024.0 / 1024.0 / 1024.0 * 4, 1); // Rough estimate
            memoryInfo.used = Math.Round(totalMemory / 1024.0 / 1024.0 / 1024.0, 1);
            memoryInfo.available = memoryInfo.total - memoryInfo.used;
            memoryInfo.usage = memoryInfo.total > 0 ? Math.Round((memoryInfo.used / memoryInfo.total) * 100, 1) : 0;
        }
        catch
        {
            // Final fallback
            memoryInfo.total = 8.0;
            memoryInfo.used = 4.0;
            memoryInfo.available = 4.0;
            memoryInfo.usage = 50.0;
        }
        
        return memoryInfo;
    }

    private StorageInfo GetStorageInfoFallback()
    {
        return GetStorageInfo(); // Storage method is already robust
    }

    private GpuInfo GetGpuInfoFallback()
    {
        var gpuInfo = new GpuInfo();
        gpuInfo.model = "Unknown GPU";
        gpuInfo.memory = 0;
        gpuInfo.usage = 0;
        return gpuInfo;
    }

    private NetworkInfo GetNetworkInfoFallback()
    {
        return GetNetworkInfo(); // Network method is already robust
    }

    private OsInfo GetOsInfoFallback()
    {
        var osInfo = new OsInfo();
        osInfo.name = "Windows";
        osInfo.version = Environment.OSVersion.Version.ToString();
        osInfo.architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        osInfo.uptime = Environment.TickCount64 / 1000;

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key != null)
            {
                var productName = key.GetValue("ProductName")?.ToString();
                var displayVersion = key.GetValue("DisplayVersion")?.ToString();
                
                if (!string.IsNullOrEmpty(productName))
                    osInfo.name = productName;
                if (!string.IsNullOrEmpty(displayVersion))
                    osInfo.version = displayVersion;
            }
        }
        catch
        {
            // Use defaults if registry access fails
        }

        return osInfo;
    }

    private MotherboardInfo GetMotherboardInfoFallback()
    {
        var motherboardInfo = new MotherboardInfo();
        motherboardInfo.manufacturer = "Unknown";
        motherboardInfo.model = "Unknown";
        return motherboardInfo;
    }
    
    // Cleanup method to dispose of static resources
    public static void Cleanup()
    {
        lock (_cpuCounterLock)
        {
            _cpuCounter?.Dispose();
            _cpuCounter = null;
        }
        
        // Clear cache
        _cachedCpuInfo = null;
        _cachedGpuInfo = null;
        _cachedMotherboardInfo = null;
    }
} 