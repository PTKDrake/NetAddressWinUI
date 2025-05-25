using System.Net.Http;

namespace NetAddressWinUI.Services;

public interface IHttpService
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default) where T : class;
    Task<T?> PostAsync<T>(string endpoint, object? data = null, CancellationToken cancellationToken = default) where T : class;
    Task<T?> PostAsJsonAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default) where T : class;
    Task<bool> PostAsync(string endpoint, object? data = null, CancellationToken cancellationToken = default);
    Task<string> PostAsStringAsync(string endpoint, object data, CancellationToken cancellationToken = default);
    void SetBearerToken(string? token);
    void SetBaseAddress(string baseUrl);
    Task<bool> TestConnectionAsync();
}

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public int StatusCode { get; set; }
} 