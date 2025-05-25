using System.Diagnostics;
using NetAddressWinUI.Models;
using NetAddressWinUI.Services;

namespace NetAddressWinUI.Service;

public partial class AuthService : IAuthService
{
    private readonly IHttpService _httpService;

    public AuthService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public async Task Logout()
    {
        try
        {
            _httpService.SetBearerToken(Settings.Token);
            var logoutRequest = new LogoutRequest { Token = Settings.Token ?? string.Empty };
            var success = await _httpService.PostAsync("/auth/revoke-session", logoutRequest);
            
            if (!success)
            {
                throw new Exception("Logout error!");
            }
        }
        finally
        {
            Settings.Token = null;
            Settings.UserId = null;
        }
    }

    public async Task<NetAddressWinUI.Models.SessionContext?> GetSessionContext()
    {
        try
        {
            _httpService.SetBearerToken(Settings.Token);
            return await _httpService.GetAsync<NetAddressWinUI.Models.SessionContext>("/auth/get-session");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting session context: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> ValidateToken()
    {
        if (Settings.Token == null)
            return false;
        try
        {
            if(await GetSessionContext() != null)
                return true;

            Settings.Token = null;
            Settings.UserId = null;
            return false;
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception.Message);
            return false;
        }
    }
}

