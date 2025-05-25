namespace NetAddressWinUI.Service;

public interface IWebSocketService
{
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default);
    Task SendMessageAsync<T>(T message, CancellationToken cancellationToken = default);
    event EventHandler<string> MessageReceived;
    Task DisconnectAsync();
}

public class WSMessage
{
    public string messageType;
    public string message;
    public WSMData? data = null;
    public string? command = null;
}

public class WSMData
{
    public string computerId;
}
