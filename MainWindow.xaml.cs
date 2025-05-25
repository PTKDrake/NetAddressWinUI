using System.Diagnostics;
using CommunityToolkit.WinUI.Behaviors;
using DevWinUI;
using Microsoft.UI.Windowing;
using NetAddressWinUI.Service;
using AutoSuggestBoxHelper = DevWinUI.AutoSuggestBoxHelper;

#nullable enable

namespace NetAddressWinUI.Views
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }
        public ModernSystemMenu ModernSystemMenu;
        private NavigationView _navigationView;

        public MainWindow(bool LoggedIn)
        {
            ViewModel = App.GetService<MainViewModel>();
            this.InitializeComponent();
                _navigationView = NavView;
            if (!LoggedIn)
            {
                RootContent.Content = new LoginPage();
            }
            else
            {
                Debug.WriteLine("okkk");
                ShowUpComponents();
            }

            ExtendsContentIntoTitleBar = true;
            ModernSystemMenu = new ModernSystemMenu(this, TitlebarMenuFlyout);
            SetTitleBar(AppTitleBar);
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

            ((OverlappedPresenter)AppWindow.Presenter).PreferredMinimumWidth = 800;
            ((OverlappedPresenter)AppWindow.Presenter).PreferredMinimumHeight = 600;

            if (App.GetService<IJsonNavigationService>() is JsonNavigationService navService)
            {
                navService.Initialize(_navigationView, NavFrame, NavigationPageMappings.PageDictionary)
                    .ConfigureDefaultPage(typeof(HomePage))
                    .ConfigureSettingsPage(typeof(SettingsPage))
                    .ConfigureJsonFile("Assets/NavViewMenu/AppData.json")
                    .ConfigureTitleBar(AppTitleBar)
                    .ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappings.PageDictionary);
            }
        }

        public void ShowUpComponents()
        {
            RootContent.Content = _navigationView;
            AppTitleBar.IsPaneToggleButtonVisible = true;
            SuggestBox.Visibility = Visibility.Visible;
        }

        public void ShowLogin()
        {
            RootContent.Content = new LoginPage();
            AppTitleBar.IsPaneToggleButtonVisible = true;
            SuggestBox.Visibility = Visibility.Visible;
        }


        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeService.ChangeThemeWithoutSave(App.MainWindow);
        }

        private void OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxTextChangedEvent(sender, args, NavFrame);
        }

        private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxQuerySubmittedEvent(sender, args, NavFrame);
        }
    }
}