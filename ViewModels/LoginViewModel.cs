using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using NetAddressWinUI.Models;
using NetAddressWinUI.Service;
using NetAddressWinUI.Services;
using Newtonsoft.Json;

namespace NetAddressWinUI.ViewModels;

public partial class LoginViewModel : ObservableValidator
{
    private readonly IHttpService _httpService;
    private readonly IAuthService _authService;

    public LoginViewModel(IHttpService httpService, IAuthService authService)
    {
        _httpService = httpService;
        _authService = authService;
        
        // Debug current settings
        Debug.WriteLine($"LoginViewModel: Current API URL = {Settings.ApiUrl}");
        Debug.WriteLine($"LoginViewModel: Current Web URL = {Settings.WebUrl}");
        Debug.WriteLine($"LoginViewModel: Current WebSocket URL = {Settings.WebSocketUrl}");
        
        // Force correct API URL if needed
        if (!Settings.ApiUrl.Contains("/api"))
        {
            Debug.WriteLine("LoginViewModel: Fixing API URL - adding /api suffix");
            Settings.ApiUrl = Settings.ApiUrl.TrimEnd('/') + "/api";
            Debug.WriteLine($"LoginViewModel: Updated API URL = {Settings.ApiUrl}");
        }
        
        // Test API connection on startup
        Task.Run(async () =>
        {
            var connectionTest = await _httpService.TestConnectionAsync();
            Debug.WriteLine($"API Connection test result: {connectionTest}");
        });
    }

    [ObservableProperty] [NotifyDataErrorInfo] [Required(ErrorMessage = "Email cannot be blank.")]
    private string email;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Password cannot be blank.")]
    [MinLength(8, ErrorMessage = "Password must have at least 8 characters.")]
    private string password;

    public bool NotErrors => !HasErrors;

    [ObservableProperty] private bool unauth = true;

    public AuthFailContent? AuthFailContent = null;
    public NetAddressWinUI.Models.SessionContext? SessionContext = null;

    public async Task LoginWithGoogle()
    {
        try
        {
            var authResult = await AuthenticateWithGoogle();
            if(authResult.IdToken != "" &&  authResult.AccessToken != "" && authResult.RefreshToken != "")
            {
                SocialAuthContent authContent = new()
                {
                    IdToken = new()
                    {
                        AccessToken = authResult.AccessToken,
                        Token = authResult.IdToken,
                        RefreshToken = authResult.RefreshToken,
                        Nonce = "PTKDrakeizdabezt"
                    }
                };

                var sessionData = await _httpService.PostAsStringAsync("/auth/sign-in/social", authContent);
                SaveSession(sessionData);
            }
            else
            {
                AuthFail(new()
                {
                    Code = "AUTH_FAILED",
                    Message = "Failed to get authentication tokens from Google"
                });
            }
        }
        catch (HttpRequestException httpEx)
        {
            Debug.WriteLine($"HTTP Error: {httpEx.Message}");
            AuthFail(new()
            {
                Code = "HTTP_ERROR",
                Message = $"Server communication error: {httpEx.Message}"
            });
        }
        catch (JsonException jsonEx)
        {
            Debug.WriteLine($"JSON Error: {jsonEx.Message}");
            AuthFail(new()
            {
                Code = "JSON_ERROR",
                Message = "Server returned invalid response format"
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"General Error: {ex.Message}");
            AuthFail(new()
            {
                Code = "UNKNOWN",
                Message = ex.Message
            });
        }
    }

    public async Task LoginWithEmailPassword()
    {
        try
        {
            var loginRequest = new LoginRequest { Email = email, Password = password };
            var sessionData = await _httpService.PostAsStringAsync("/auth/sign-in/email", loginRequest);
            SaveSession(sessionData);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            AuthFail(new AuthFailContent
            {
                Code = "ERROR",
                Message = ex.Message
            });
        }
    }

    private void AuthFail(AuthFailContent authFailContent)
    {
        Debug.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(authFailContent));
        SessionContext = null;
        AuthFailContent = authFailContent;
        Unauth = true;
        Settings.Token = null;
    }

    private async Task<WinUIEx.WebAuthenticatorResult> AuthenticateWithGoogle()
    {
#if !IS_PACKAGED
        try
        {
            Microsoft.Windows.AppLifecycle.ActivationRegistrationManager.RegisterForProtocolActivation(
                Settings.RedirectionProtocol, "Assets\\Store\\Square150x150Logo.scale-200",
                ProcessInfoHelper.ProductName, null);
#endif
            string redirectUri = $"{Settings.ApiUrl}/computers/oauth";
            var clientId = "43702497942-5pn8umove7g98qpastar72atie7plu6v.apps.googleusercontent.com";
            var requestUri =
                new Uri(
                    $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope=email%20profile&access_type=offline&prompt=consent");
            var callbackUri = new Uri($"{Settings.RedirectionProtocol}://oauth/google");
            return await WinUIEx.WebAuthenticator.AuthenticateAsync(requestUri, callbackUri);
#if !IS_PACKAGED
        }
        finally
        {
            Microsoft.Windows.AppLifecycle.ActivationRegistrationManager.UnregisterForProtocolActivation(
                Settings.RedirectionProtocol, null);
        }
#endif
    }

    private void SaveSession(string stringContext)
    {
        try
        {
            AuthFailContent = null;
            SessionContext = JsonConvert.DeserializeObject<NetAddressWinUI.Models.SessionContext>(stringContext);
            Settings.Token = SessionContext?.Token;
            Settings.UserId = SessionContext?.User?.Id;
            Unauth = false;
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"Failed to parse session response: {ex.Message}");
            Debug.WriteLine($"Response content: {stringContext}");
            throw new JsonException("Failed to parse authentication response from server", ex);
        }
    }
}