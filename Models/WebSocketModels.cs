namespace NetAddressWinUI.Models;

public class WSMessage
{
    public string MessageType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public WSMData? Data { get; set; }
    public string? Command { get; set; }
}

public class WSMData
{
    public string ComputerId { get; set; } = string.Empty;
} 