using H.NotifyIcon;

namespace NetAddressWinUI.Views
{
    public sealed partial class HomePage : Page
    {
        public HomeViewModel ViewModel;
        public HomePage()
        {
            this.InitializeComponent();
            ViewModel = App.GetService<HomeViewModel>();
            DataContext = ViewModel;
        }
    }

}
