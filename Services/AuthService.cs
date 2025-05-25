using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace NetAddressWinUI.Service;

public partial class AuthService : IAuthService
{
    private readonly HttpClient _httpClient = new();

    public AuthService()
    {
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private void SetBearerToken(string token)
    {
        // Xóa token cũ nếu có
        _httpClient.DefaultRequestHeaders.Authorization = null;

        // Thêm Bearer token vào header
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task Logout()
    {
        SetBearerToken(Settings.Token);
        var data = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string, string>()
        {
            { "token", Settings.Token }
        }), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{Settings.ApiUrl}/auth/revoke-session", data);
        if (response.IsSuccessStatusCode)
        {
            return;
        }
        else
        {
            Debug.WriteLine(await response.Content.ReadAsStringAsync());
        }

        Settings.Token = null;
        Settings.UserId = null;

        throw new Exception("Logout error!");
    }

    public async Task<SessionContext?> GetSessionContext()
    {
        SetBearerToken(Settings.Token);
        var response = await _httpClient.GetAsync($"{Settings.ApiUrl}/auth/get-session");
        if (response.IsSuccessStatusCode)
        {
            if (await response.Content.ReadAsStringAsync() != "null")
            {
                var Context = Newtonsoft.Json.JsonConvert.DeserializeObject<SessionContext>(await response.Content.ReadAsStringAsync());
                return Context;
            }
            else
            {
                Debug.WriteLine("wrong token");
            }
        }
        else
        {
            Debug.WriteLine(await response.Content.ReadAsStringAsync());
        }

        return null;
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

