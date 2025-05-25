using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json;

namespace NetAddressWinUI.Services;

public class HttpService : IHttpService, IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed = false;

    public HttpService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        // Ensure base URL ends with trailing slash to prevent URI construction issues
        var baseUrl = Settings.ApiUrl.TrimEnd('/') + "/";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public void SetBearerToken(string? token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public void SetBaseAddress(string baseUrl)
    {
        // Only set if not already set to prevent the exception
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // Remove leading slash to prevent URI construction issues
            var cleanEndpoint = endpoint.TrimStart('/');
            var response = await _httpClient.GetAsync(cleanEndpoint, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrEmpty(content) || content == "null")
                    return null;
                
                return JsonConvert.DeserializeObject<T>(content);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"HTTP GET Exception: {ex.Message}");
            return null;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? data = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // Remove leading slash to prevent URI construction issues
            var cleanEndpoint = endpoint.TrimStart('/');
            
            HttpContent? content = null;
            if (data != null)
            {
                var json = JsonConvert.SerializeObject(data);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.PostAsync(cleanEndpoint, content, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrEmpty(responseContent) || responseContent == "null")
                    return null;
                
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"HTTP POST Exception: {ex.Message}");
            return null;
        }
    }

    public async Task<T?> PostAsJsonAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // Remove leading slash to prevent URI construction issues
            var cleanEndpoint = endpoint.TrimStart('/');
            var response = await _httpClient.PostAsJsonAsync(cleanEndpoint, data, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrEmpty(content) || content == "null")
                    return null;
                
                return JsonConvert.DeserializeObject<T>(content);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"HTTP POST JSON Exception: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> PostAsync(string endpoint, object? data = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Remove leading slash to prevent URI construction issues
            var cleanEndpoint = endpoint.TrimStart('/');
            
            HttpContent? content = null;
            if (data != null)
            {
                var json = JsonConvert.SerializeObject(data);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.PostAsync(cleanEndpoint, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"HTTP POST Exception: {ex.Message}");
            return false;
        }
    }

    public async Task<string> PostAsStringAsync(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            // Remove leading slash to prevent URI construction issues
            var cleanEndpoint = endpoint.TrimStart('/');
            var response = await _httpClient.PostAsJsonAsync(cleanEndpoint, data, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                // Check if response is HTML (error page)
                if (content.TrimStart().StartsWith("<"))
                {
                    throw new HttpRequestException($"Server returned HTML error page. Status: {response.StatusCode}");
                }
                
                throw new HttpRequestException($"Request failed with status {response.StatusCode}: {content}");
            }
            
            // Validate that response is JSON before returning
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new HttpRequestException("Server returned empty response");
            }
            
            // Check if response starts with '<' (HTML) instead of JSON
            if (content.TrimStart().StartsWith("<"))
            {
                var fullUrl = new Uri(_httpClient.BaseAddress, cleanEndpoint);
                throw new HttpRequestException($"Server returned HTML instead of JSON. Check API endpoint: {fullUrl}. Status: {response.StatusCode}");
            }
            
            return content;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"HTTP POST String Exception: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("", CancellationToken.None);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Connection test failed: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
} 