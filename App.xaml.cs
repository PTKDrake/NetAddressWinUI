#nullable enable

using System.Diagnostics;
using System.Threading;
using Windows.ApplicationModel.Activation;
using Windows.System;
using Microsoft.Windows.AppLifecycle;
using Microsoft.UI.Xaml;
using NetAddressWinUI.Service;
using WinRT.Interop;
using WinUIEx;

namespace NetAddressWinUI
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public static MainWindow MainWindow = Window.Current as MainWindow;
        public TaskbarIcon? TrayIcon { get; set; } = null;
        public static bool HandleClosedEvents { get; set; } = false;
        public static IntPtr Hwnd => WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
        public IServiceProvider Services { get; }
        public IJsonNavigationService NavService => GetService<IJsonNavigationService>();
        public IThemeService ThemeService => GetService<IThemeService>();
        public IAuthService AuthService => GetService<IAuthService>();


        public static T GetService<T>() where T : class
        {
            if ((App.Current as App)!.Services.GetService(typeof(T)) is not T service)
            {
                throw new ArgumentException(
                    $"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
            }

            return service;
        }

        public App()
        {
#if !IS_PACKAGED
            ActivationRegistrationManager.UnregisterForProtocolActivation(
                Settings.RedirectionProtocol, null);
#endif
            if (WebAuthenticator.CheckOAuthRedirectionActivation())
                return;
            Services = ConfigureServices();
            // Enables Multicore JIT with the specified profile
            System.Runtime.ProfileOptimization.SetProfileRoot(Constants.RootDirectoryPath);
            System.Runtime.ProfileOptimization.StartProfile("Startup.Profile");
            this.InitializeComponent();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IJsonNavigationService, JsonNavigationService>();
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IWebSocketService, WebSocketService>();

            services.AddTransient<MainViewModel>();
            services.AddTransient<HomeViewModel>();
            services.AddTransient<GeneralSettingViewModel>();
            services.AddTransient<AboutUsSettingViewModel>();
            services.AddTransient<AccountSettingViewModel>();
            services.AddTransient<LoginViewModel>();

            return services.BuildServiceProvider();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            InitializeMainWindow();
        }

        public async void InitializeMainWindow()
        {
            bool loggedIn = await AuthService.ValidateToken();
            Debug.WriteLine(loggedIn);
            MainWindow = new MainWindow(loggedIn);
            InitializeTheme();

            MainWindow.Title = MainWindow.AppWindow.Title = ProcessInfoHelper.ProductNameAndVersion;
            MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");


            MainWindow.Closed += async (sender, args) =>
            {
                if (HandleClosedEvents && Settings.Token != null)
                {
                    args.Handled = true;
                    MainWindow.Hide();
                }
                else
                {
                    TrayIcon?.Dispose();
                    Environment.Exit(0);
                }
            };
            InitializeTrayIcon();
            MainWindow.Activate();
        }

        private void InitializeTheme()
        {
            ThemeService.Initialize(MainWindow, true, Constants.ThemeConfigPath);
            ThemeService.ConfigureBackdrop(BackdropType.AcrylicBase);
            ThemeService.ConfigureElementTheme(ElementTheme.Default);
            ThemeService.ConfigureTintColor(Windows.UI.Color.FromArgb(255, 0, 120, 212));
            ThemeService.ConfigureFallbackColor(Windows.UI.Color.FromArgb(255, 0, 120, 212));
        }

        public void InitializeTrayIcon()
        {
            if (!Settings.UseTrayIcon)
                return;

            HandleClosedEvents = Settings.UseTrayIcon;
            var showHideWindowCommand = (XamlUICommand)Resources["ShowHideWindowCommand"];
            showHideWindowCommand.ExecuteRequested += ShowHideWindowCommand_ExecuteRequested;
            var exitApplicationCommand = (XamlUICommand)Resources["ExitApplicationCommand"];
            exitApplicationCommand.ExecuteRequested += ExitApplicationCommand_ExecuteRequested;
            TrayIcon = (TaskbarIcon)Resources["TrayIcon"];
            TrayIcon.ForceCreate();
        }

        private void ShowHideWindowCommand_ExecuteRequested(object? _, ExecuteRequestedEventArgs args)
        {
            if (MainWindow.Visible)
            {
                MainWindow.Hide();
            }
            else
            {
                MainWindow.Show();
            }
        }

        private void ExitApplicationCommand_ExecuteRequested(object? _, ExecuteRequestedEventArgs args)
        {
            HandleClosedEvents = false;
            TrayIcon?.Dispose();
            MainWindow?.Close();

            // https://github.com/HavenDV/H.NotifyIcon/issues/66
            if (MainWindow == null)
            {
                Environment.Exit(0);
            }
        }
    }
}