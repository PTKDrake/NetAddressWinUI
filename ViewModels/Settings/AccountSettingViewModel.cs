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
        var user = (await App.GetService<IAuthService>().GetSessionContext())?.user;
        if (user != null)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Email = user.email;
                LastName = user.lastName;
                FirstName = user.firstName;
                UserId = user.id;
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