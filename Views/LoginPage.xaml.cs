using System.Diagnostics;
using CommunityToolkit.WinUI.Behaviors;

namespace NetAddressWinUI.Views;

public partial class LoginPage : Page
{
    public LoginViewModel ViewModel { get; }

    public LoginPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<LoginViewModel>();
        DataContext = ViewModel;
        ViewModel.Email = "";
        ViewModel.Password = "";
        SignUp.NavigateUri = new Uri($"{Settings.WebUrl}/signup");
        ForgotPassword.NavigateUri = new Uri($"{Settings.WebUrl}/forgot-password");
    }

    private async void EmailLogin(object sender, RoutedEventArgs e)
    {
        Button button = (Button)sender;
        if (TextBoxValidation())
        {
            button.IsEnabled = false;
            await ViewModel.LoginWithEmailPassword();
            if (ViewModel.Unauth)
            {
                Growl.ErrorGlobal("Login failed!", ViewModel.AuthFailContent?.Message ?? "Unknown error.");
            }
            else
            {
                Growl.SuccessGlobal("Login success!", $"Wellcome, {ViewModel.SessionContext?.User?.Name}.");
                App.MainWindow.ShowUpComponents();
            }

            button.IsEnabled = true;
        }
    }

    private async void GoogleLogin(object sender, RoutedEventArgs e)
    {
        Button button = (Button)sender;
        button.IsEnabled = false;
        await ViewModel.LoginWithGoogle();
        if (ViewModel.Unauth)
        {
            Growl.ErrorGlobal("Login failed!", ViewModel.AuthFailContent?.Message ?? "Unknown error.");
        }
        else
        {
            Growl.SuccessGlobal("Login success!", $"Wellcome, {ViewModel.SessionContext?.User?.Name}.");
            App.MainWindow.ShowUpComponents();
        }

        button.IsEnabled = true;
    }

    private bool TextBoxValidation(ValidateTarget target = ValidateTarget.All)
    {
        if (target != ValidateTarget.Password)
        {
            ViewModel.Email = EmailBox.Text;
            var errors = ViewModel.GetErrors(nameof(ViewModel.Email));
            if (errors.Any())
            {
                EmailError.Visibility = Visibility.Visible;
                EmailErrorTooltip.Content = errors.First().ErrorMessage;
                return false;
            }
            else
            {
                EmailError.Visibility = Visibility.Collapsed;
            }
        }

        if (target != ValidateTarget.Email)
        {
            ViewModel.Password = PasswordBox.Password;
            var errors = ViewModel.GetErrors(nameof(ViewModel.Password));
            if (errors.Any())
            {
                PasswordError.Visibility = Visibility.Visible;
                PasswordErrorTooltip.Content = errors.First().ErrorMessage;
                return false;
            }
            else
            {
                PasswordError.Visibility = Visibility.Collapsed;
            }
        }

        return true;
    }

    private void EmailBox_OnLosingFocus(object o, RoutedEventArgs routedEventArgs)
    {
        TextBoxValidation(ValidateTarget.Email);
    }

    private void PasswordBox_OnLosingFocus(UIElement sender, LosingFocusEventArgs args)
    {
        TextBoxValidation(ValidateTarget.Password);
    }
}

enum ValidateTarget
{
    All,
    Email,
    Password
}