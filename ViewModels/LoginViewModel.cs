using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace NetAddressWinUI.ViewModels;

public partial class LoginViewModel : ObservableValidator
{
    private readonly HttpClient _httpClient = new();

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
    public SessionContext? SessionContext = null;

    public async Task LoginWithGoogle()
    {
        try
        {
            var authResult = await AuthenticateWithGoogle();
            if(authResult.IdToken != "" &&  authResult.AccessToken != "" && authResult.RefreshToken != "")
            {
                SocialAuthContent authContent = new()
                {
                    idToken = new()
                    {
                        accessToken = authResult.AccessToken,
                        token = authResult.IdToken,
                        refreshToken = authResult.RefreshToken,
                        nonce = "PTKDrakeizdabezt"
                    }
                };

                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(authContent), Encoding.UTF8,
                    "application/json");
                var response = await _httpClient.PostAsync($"{Settings.ApiUrl}/auth/sign-in/social", content);
                if (response.IsSuccessStatusCode)
                {
                    SaveSession(await response.Content.ReadAsStringAsync());
                }
                else await HandleAuthFail(response);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            AuthFail(new()
            {
                code = "UNKNOWN",
                message = ex.Message
            });
        }
    }

    public async Task LoginWithEmailPassword()
    {
        var response =
            await _httpClient.PostAsJsonAsync($"{Settings.ApiUrl}/auth/sign-in/email", new { email, password });
        if (response.IsSuccessStatusCode)
        {
            SaveSession(await response.Content.ReadAsStringAsync());
        }
        else await HandleAuthFail(response);
    }

    private async Task HandleAuthFail(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            AuthFail(new ()
            {
                code = "NOT_FOUND",
                message = "Server error! Please try later."
            });
        }
        else
        {
            AuthFail(await response.Content.ReadFromJsonAsync<AuthFailContent>());
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
        AuthFailContent = null;
        SessionContext = Newtonsoft.Json.JsonConvert.DeserializeObject<SessionContext>(stringContext);
        Settings.Token = SessionContext.token;
        Settings.UserId = SessionContext.user.id;
        Unauth = false;
    }
}

public class SocialAuthContent
{
    public string provider = "google";
    public IdToken idToken;
}

public class IdToken
{
    public string token = "";
    public string accessToken = "";
    public string refreshToken = "";
    public string nonce = "";
}

public class AuthFailContent
{
    public string code;
    public string message;
}

public class SessionContext
{
    public bool redirect;
    public string token;
    public User user = new ();
}

public class User
{
    public string id;
    public string email;
    public string name;
    public string image;
    public bool emailVerified;
    public string createdAt;
    public string updatedAt;
}