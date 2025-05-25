using Windows.UI;
using System.Collections.ObjectModel;

namespace NetAddressWinUI.Views
{
    public sealed partial class ThemeSettingPage : Page
    {
        public ThemeSettingPage()
        {
            this.InitializeComponent();
            UpdateColorControls();
        }

        private void UpdateColorControls(byte sender = 0)
        {
            if (sender == 1 && ColorPickerFlyout.IsOpen)
                return;

            Color? color = ThemeSettings.BackdropTintColor;
            if (color is not null)
            {
                if (sender != 2)
                    foreach (ColorPaletteItem item in
                             ((ObservableCollection<ColorPaletteItem>)ColorPalette.ItemsSource))
                    {
                        if (item.Color == color)
                        {
                            ColorPalette.SelectedItem = item;
                            TintBox.Fill = new SolidColorBrush((Color)color);
                            return;
                        }
                    }

                ColorPalette.SelectedItem = null;
            }
            else
            {
                ColorPalette.SelectedIndex = 0;
            }

            TintBox.Fill = new SolidColorBrush(color ?? Colors.Transparent);
        }

        private void OnDropDownClosed(object sender, object e)
        {
            string backdropName = ((ComboBoxItem)((ComboBox)sender).SelectedItem).Tag.ToString();
            if (Enum.TryParse(backdropName, out BackdropType backdropType) &&
                Enum.IsDefined(typeof(BackdropType), backdropType))
            {
                switch (backdropType)
                {
                    case BackdropType.AcrylicBase:
                    case BackdropType.AcrylicThin:
                    case BackdropType.Mica:
                    case BackdropType.MicaAlt:
                        if (ThemeSettings.BackdropTintColor != null)
                            App.GetService<IThemeService>().SetBackdropTintColor(ThemeSettings.BackdropTintColor);
                        break;
                }
            }
        }

        private void OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            ThemeSettings.BackdropTintColor = args.NewColor;
            App.Current.ThemeService.ConfigureTintColor(args.NewColor, true);
            UpdateColorControls(1);
        }

        private void OnColorPaletteItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ColorPaletteItem color)
            {
                if (color.Hex.Contains("#000000"))
                {
                    App.Current.ThemeService.ResetBackdropProperties();
                }
                else
                {
                    App.Current.ThemeService.ConfigureTintColor(color.Color, true);
                    ThemeSettings.BackdropTintColor = color.Color;
                }

                UpdateColorControls(2);
            }
        }

        private void ColorPickerFlyout_OnClosed(object sender, object e)
        {
            UpdateColorControls(1);
        }
    }
}