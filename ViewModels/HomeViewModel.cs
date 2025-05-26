using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Windows.System;
using NetAddressWinUI.Service;
using NetAddressWinUI.Services;
using NetAddressWinUI.Models;
using Newtonsoft.Json;
using Microsoft.UI.Xaml;

namespace NetAddressWinUI.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    public string MachineName => Environment.MachineName;
    
    [ObservableProperty]
    private string ipAddresses;
    
    [ObservableProperty]
    private string macAddresses;
    
    [ObservableProperty]
    private string ipAddress;
    
    [ObservableProperty]
    private string macAddress;
    
    [ObservableProperty] 
    private bool connected = false;
    
    [ObservableProperty]
    private HardwareInfo hardwareInfo = new();
    
    [ObservableProperty]
    private string lastUpdateTime = "Never";
    
    public string FormattedUptime => FormatUptime(HardwareInfo.os.uptime);
    
    partial void OnHardwareInfoChanged(HardwareInfo value)
    {
        OnPropertyChanged(nameof(FormattedUptime));
    }
    
    private readonly IWebSocketService _webSocketService;
    private readonly IHardwareInfoService _hardwareInfoService;
    private readonly IHttpService _httpService;
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private readonly DispatcherTimer _hardwareUpdateTimer = new();

    public HomeViewModel(IWebSocketService webSocketService, IHardwareInfoService hardwareInfoService, IHttpService httpService)
    {
        Debug.WriteLine(App.Hwnd.ToString());
        _webSocketService = webSocketService;
        _hardwareInfoService = hardwareInfoService;
        _httpService = httpService;
        _webSocketService.MessageReceived += (sender, s) =>
        {
            var json = JsonConvert.DeserializeObject<NetAddressWinUI.Models.WSMessage>(s);
            if (json.MessageType == "info")
            {
                if (json.Message == "registered")
                {
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        Connected = true;
                        Growl.SuccessGlobal("Success", "Registered machine to the server!");
                    });
                } else

                if (json.Message == "disconnected")
                {
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        Connected = false;
                        Growl.SuccessGlobal("Disconnected", "Disconnected from the server!");
                    });
                } else

                if (json.Message == "connected")
                {
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        Connected = true;
                        Growl.SuccessGlobal("Connected", "Connected to the server!");
                    });
                } else

                if (json.Message == "updated")
                {
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        // Không hiển thị notification cho update để tránh spam
                        Debug.WriteLine("Hardware information updated successfully");
                    });
                }
            }
            else if (json.MessageType == "command")
            {
                if (json.Message == "shutdown")
                {
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        if (Settings.RealShutDown)
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "shutdown",
                                Arguments = $"-s -t -f 90",
                                CreateNoWindow = true,
                                UseShellExecute = false
                            });
                            Growl.WarningGlobal("Shutting down", "Your machine will shut down after 90 seconds!");
                            MessageBox.ShowWarning(App.MainWindow, "Your machine will shut down after 90 seconds!",
                                "Shutting down");
                        }
                        else
                            Growl.WarningGlobal("Shutting down", "Trigger fake shut down!");

                        Connected = false;
                    });
                }
            }
        };

        // Initialize IP address asynchronously to get public IP
        Task.Run(async () =>
        {
            try
            {
                // Try to get public IP address first
                var publicIp = await _httpService.GetPublicIpAddressAsync();
                if (!string.IsNullOrEmpty(publicIp))
                {
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        IpAddress = publicIp;
                        Debug.WriteLine($"Public IP address obtained: {publicIp}");
                    });
                }
                else
                {
                    // Fallback to local IP if public IP fails
                    var localIp = GetLocalIpAddress();
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        IpAddress = localIp ?? "Unknown";
                        Debug.WriteLine($"Using local IP address: {localIp}");
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting IP address: {ex.Message}");
                // Fallback to local IP
                var localIp = GetLocalIpAddress();
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    IpAddress = localIp ?? "Unknown";
                });
            }
        });

        System.Net.NetworkInformation.NetworkInterface[] adapters = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().Where(e => e.OperationalStatus == OperationalStatus.Up && e.NetworkInterfaceType != NetworkInterfaceType.Loopback).ToArray();
        Debug.WriteLine(JsonConvert.SerializeObject(adapters.Select(e => e.GetPhysicalAddress().ToString()).Cast<String>()));
        foreach (var adapter in adapters)
        {
            if (adapter.OperationalStatus == OperationalStatus.Up &&
                adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                PhysicalAddress mac = adapter.GetPhysicalAddress();
                byte[] bytes = mac.GetAddressBytes();
                MacAddress = string.Join(":", bytes.Select(b => b.ToString("X2")));
                break;
            }
        }
        
        // Initialize hardware information updates
        InitializeHardwareUpdates();
    }
    
    private void InitializeHardwareUpdates()
    {
        // Load hardware info immediately on startup for faster display
        Task.Run(async () =>
        {
            try
            {
                var initialHardwareInfo = await _hardwareInfoService.GetHardwareInfoAsync();
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    HardwareInfo = initialHardwareInfo;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading initial hardware info: {ex.Message}");
            }
        });
        
        // Set up timer for regular updates (every 5 seconds for UI, every 30 seconds for server)
        _hardwareUpdateTimer.Interval = TimeSpan.FromSeconds(5);
        _hardwareUpdateTimer.Tick += (sender, e) => {
            LoadHardwareInfo();
            SendHardwareUpdate();
        };
        _hardwareUpdateTimer.Start();
    }
    
    private async void LoadHardwareInfo()
    {
        try
        {
            var newHardwareInfo = await _hardwareInfoService.GetHardwareInfoAsync();
            
            // Update on UI thread
            _dispatcherQueue?.TryEnqueue(() =>
            {
                HardwareInfo = newHardwareInfo;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading hardware info: {ex.Message}");
        }
    }
    
    private async void SendHardwareUpdate()
    {
        // Only send updates if connected to server
        if (!Connected) return;
        
        try
        {
            // Refresh IP address periodically (every 10 updates = ~5 minutes)
            if (DateTime.Now.Minute % 5 == 0)
            {
                await RefreshIpAddress();
            }
            
            // Get latest hardware information
            var hardwareInfo = HardwareInfo;
            
            var updateMessage = new UpdateMessage
            {
                messageType = "update",
                macAddress = MacAddress,
                ipAddress = IpAddress,
                machineName = MachineName,
                hardware = hardwareInfo
            };
            
            await _webSocketService.SendMessageAsync(updateMessage);
            
            // Update last update time on UI thread
            _dispatcherQueue?.TryEnqueue(() =>
            {
                LastUpdateTime = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
            });
            
            Debug.WriteLine("Hardware update sent to server");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error sending hardware update: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task Connect()
    {
        try
        {
            await _webSocketService.ConnectAsync(new Uri(Settings.WebSocketUrl), CancellationToken.None);

            // Get hardware information
            var hardwareInfo = await _hardwareInfoService.GetHardwareInfoAsync();

            var registerMessage = new RegisterMessage
            {
                messageType = "register",
                userId = Settings.UserId,
                machineName = MachineName,
                ipAddress = IpAddress,
                macAddress = MacAddress,
                hardware = hardwareInfo
            };

            await _webSocketService.SendMessageAsync(registerMessage);
            
            // Update last update time when registering
            LastUpdateTime = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    [RelayCommand]
    public async Task Disconnect()
    {
        try
        {
            await _webSocketService.ConnectAsync(new Uri(Settings.WebSocketUrl), CancellationToken.None);
            
            var disconnectMessage = new DisconnectMessage
            {
                messageType = "disconnect",
                macAddress = MacAddress
            };
            
            await _webSocketService.SendMessageAsync(disconnectMessage);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    [RelayCommand]
    public async Task RefreshIp()
    {
        try
        {
            await RefreshIpAddress();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error refreshing IP: {ex.Message}");
        }
    }

    private async Task Shutdown()
    {
        try
        {
            await _webSocketService.ConnectAsync(new Uri(Settings.WebSocketUrl), CancellationToken.None);
            
            var shutdownMessage = new ShutdownMessage
            {
                messageType = "shutdown",
                macAddress = MacAddress
            };
            
            await _webSocketService.SendMessageAsync(shutdownMessage);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
    
    private string FormatUptime(long uptimeSeconds)
    {
        if (uptimeSeconds <= 0) return "0m";
        
        var timeSpan = TimeSpan.FromSeconds(uptimeSeconds);
        
        var days = timeSpan.Days;
        var hours = timeSpan.Hours;
        var minutes = timeSpan.Minutes;
        
        var parts = new List<string>();
        
        if (days > 0)
            parts.Add($"{days}d");
        if (hours > 0)
            parts.Add($"{hours}h");
        if (minutes > 0 || parts.Count == 0)
            parts.Add($"{minutes}m");
            
        return string.Join(" ", parts);
    }
    
    public void StopHardwareUpdates()
    {
        _hardwareUpdateTimer?.Stop();
        
        // Cleanup hardware service resources
        NetAddressWinUI.Services.HardwareInfoService.Cleanup();
    }

    private async Task RefreshIpAddress()
    {
        try
        {
            var publicIp = await _httpService.GetPublicIpAddressAsync();
            if (!string.IsNullOrEmpty(publicIp) && publicIp != IpAddress)
            {
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    IpAddress = publicIp;
                    Debug.WriteLine($"IP address updated to: {publicIp}");
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error refreshing IP address: {ex.Message}");
        }
    }

    private string? GetLocalIpAddress()
    {
        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (var address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address))
                {
                    return address.ToString();
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting local IP: {ex.Message}");
            return null;
        }
    }
}