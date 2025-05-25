using Nucs.JsonSettings.Examples;
using Nucs.JsonSettings.Modulation;

namespace NetAddressWinUI.Common
{
    [GenerateAutoSaveOnChange]
    public partial class AppConfig : NotifiyingJsonSettings, IVersionable
    {
        [EnforcedVersion("1.0.0.0")] public Version Version { get; set; } = new Version(1, 0, 0, 0);

        private string fileName { get; set; } = Constants.AppConfigPath;
        private bool useTrayIcon { get; set; } = true;
        private string webUrl { get; set; } = "http://localhost:3000";
        private string apiUrl { get; set; } = "http://localhost:3000/api";
        private string webSocketUrl { get; set; } = "ws://localhost:3000/api/websocket";
        private string? token { get; set; } = null;
        private string? userId { get; set; } = null;
        private string redirectionProtocol { get; set; } = @"netaddress";
        private bool realShutDown { get; set; } = false;
    }
}