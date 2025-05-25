using NetAddressWinUI.Service;
using System.Diagnostics;
using Windows.System;

namespace NetAddressWinUI.ViewModels;

public partial class AccountSettingViewModel: ObservableObject
{
    [ObservableProperty] private string firstName;
    [ObservableProperty] private string lastName;
    [ObservableProperty] private string email;
    [ObservableProperty] private string userId;
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public async Task UpdateData()
    {
        var sessionContext = await App.GetService<IAuthService>().GetSessionContext();
        var user = sessionContext?.User;
        if (user != null)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Email = user.Email;
                LastName = user.Name; // Using Name as LastName since there's no separate LastName
                FirstName = user.Name; // Using Name as FirstName since there's no separate FirstName
                UserId = user.Id;
            });
        }
        else
        {
            Growl.ErrorGlobal("Error", "Error when get profile data.");
        }
    }

    [RelayCommand]
    public async Task Logout()
    {
        try
        {
            await App.GetService<IAuthService>().Logout();
            Growl.SuccessGlobal("Logged out", "Logout success.");
        }
        catch (Exception ex)
        {
            Growl.ErrorGlobal("Error", ex.Message);
        }
        App.MainWindow.ShowLogin();
    }
}