using Windows.UI;
using Nucs.JsonSettings.Examples;
using Nucs.JsonSettings.Modulation;
using Nucs.JsonSettings;

namespace NetAddressWinUI.Common;

[GenerateAutoSaveOnChange]
public partial class ThemeConfig: NotifiyingJsonSettings, IVersionable
{
    [EnforcedVersion("1.0.0.0")]
    public Version Version { get; set; } = new Version(1, 0, 0, 0);
    private string fileName { get; set; } = Constants.ThemeConfigPath;
    private ElementTheme elementTheme { get; set; } = ElementTheme.Default;
    private BackdropType backdropType { get; set; } = BackdropType.Mica;
    private Color? backdropTintColor { get; set; } = null;
    private Color? backdropFallBackColor { get; set; } = null;

    private bool isThemeFirstRun { get; set; } = true;
    private bool isBackdropFirstRun { get; set; } = true;
    private bool isBackdropTintColorFirstRun { get; set; } = true;
    private bool isBackdropFallBackColorFirstRun { get; set; } = true;
}