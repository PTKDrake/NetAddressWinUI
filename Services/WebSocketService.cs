using System.Diagnostics;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Text.Json;

namespace NetAddressWinUI.Service;

public class WebSocketService : IWebSocketService
{
    private MessageWebSocket _socket;
    private DataWriter _writer;

    public event EventHandler<string> MessageReceived;

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        _socket = new MessageWebSocket();
        _socket.Control.MessageType = SocketMessageType.Utf8;
        _socket.MessageReceived += OnMessageReceived;

        // Kết nối với server
        await _socket.ConnectAsync(uri).AsTask(cancellationToken);  // :contentReference[oaicite:3]{index=3}

        // Tạo writer để gửi dữ liệu
        _writer = new DataWriter(_socket.OutputStream);
        Debug.WriteLine("connected websocket");
    }

    private async void OnMessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
    {
        using var reader = args.GetDataReader();
        reader.UnicodeEncoding = UnicodeEncoding.Utf8;
        string json = reader.ReadString(reader.UnconsumedBufferLength);
        Debug.WriteLine($"websocket: {json}");
        MessageReceived?.Invoke(this, json);
    }

    public async Task SendMessageAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        // Serialize object thành JSON  
        string json = JsonSerializer.Serialize(message);  // :contentReference[oaicite:4]{index=4}

        _writer.WriteString(json);
        await _writer.StoreAsync().AsTask(cancellationToken);
        await _writer.FlushAsync().AsTask(cancellationToken);
    }

    public Task DisconnectAsync()
    {
        _socket?.Dispose();
        _socket = null;
        _writer = null;
        return Task.CompletedTask;
    }
}