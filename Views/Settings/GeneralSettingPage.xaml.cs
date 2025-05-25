using System.Diagnostics;

namespace NetAddressWinUI.Views
{
    public sealed partial class GeneralSettingPage : Page
    {
        public GeneralSettingViewModel ViewModel { get; }
        public GeneralSettingPage()
        {
            ViewModel = App.GetService<GeneralSettingViewModel>();
            this.InitializeComponent();
        }

        private void ToggleTray_OnToggled(object sender, RoutedEventArgs e)
        {
            App.HandleClosedEvents = ToggleTray.IsOn;
            Settings.UseTrayIcon = ToggleTray.IsOn;
            if(ToggleTray.IsOn)
            {
                if (App.Current.TrayIcon is null)
                {
                    App.Current.InitializeTrayIcon();
                }
                else
                    App.Current.TrayIcon.TrayIcon.Show();
            }
            else
            {
                App.Current.TrayIcon?.TrayIcon.Hide();
            }
        }
    }


}
