using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Windows.System;
using NetAddressWinUI.Service;
using Newtonsoft.Json;
using Microsoft.UI.Xaml;

namespace NetAddressWinUI.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    public string MachineName => Environment.MachineName;
    public string IpAddresses;
    public string MacAddresses;
    public string IpAddress;
    public string MacAddress;
    [ObservableProperty] private bool connected = false;
    private readonly IWebSocketService _webSocketService;
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();


    public HomeViewModel()
    {
        Debug.WriteLine(App.Hwnd.ToString());
        _webSocketService = App.GetService<IWebSocketService>();
        _webSocketService.MessageReceived += (sender, s) =>
        {
            var json = JsonConvert.DeserializeObject<WSMessage>(s);
            if (json.messageType == "info")
            {
                if (json.message == "registered")
                {
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        Connected = true;
                        Growl.SuccessGlobal("Success", "Registered machine to the server!");
                    });
                } else

                if (json.message == "disconnected")
                {
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        Connected = false;
                        Growl.SuccessGlobal("Disconnected", "Disconnected from the server!");
                    });
                } else

                if (json.message == "connected")
                {
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        Connected = true;
                        Growl.SuccessGlobal("Connected", "Connected to the server!");
                    });
                }
            }
            else if (json.messageType == "command")
            {
                if (json.message == "shutdown")
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

        IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
        Debug.WriteLine(JsonConvert.SerializeObject(addresses.Select(e => e.ToString()).Cast<String>()));
        foreach (var address in addresses)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                IpAddress = address.ToString();
                break;
            }
        }

        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces().Where(e => e.OperationalStatus == OperationalStatus.Up && e.NetworkInterfaceType != NetworkInterfaceType.Loopback).ToArray();
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
    }

    [RelayCommand]
    public async Task Connect()
    {
        try
        {
            await _webSocketService.ConnectAsync(new Uri(Settings.WebSocketUrl), CancellationToken.None);

            var data = new Dictionary<string, string>()
            {
                { "messageType", "register" },
                { "userId", Settings.UserId },
                { "machineName", MachineName },
                { "ipAddress", IpAddress },
                { "macAddress", MacAddress },
                {"test", "1234"}
            };
            await _webSocketService.SendMessageAsync(data);
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
            var data = new Dictionary<string, string>()
            {
                { "messageType", "disconnect" },
                { "macAddress", MacAddress }
            };
            await _webSocketService.SendMessageAsync(data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private async Task Shutdown()
    {
        try
        {
            await _webSocketService.ConnectAsync(new Uri(Settings.WebSocketUrl), CancellationToken.None);
            var data = new Dictionary<string, string>()
            {
                { "messageType", "shutdown" },
                { "macAddress", MacAddress }
            };
            await _webSocketService.SendMessageAsync(data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
}