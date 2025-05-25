namespace NetAddressWinUI.Service;

public interface IAuthService
{
    Task<SessionContext> GetSessionContext();
    Task<bool> ValidateToken();
    Task Logout();
}

public class SessionContext
{
    public Session session = new();
    public User user = new();
}

public class Session
{
    public string expiresAt;
    public string token;
    public string createdAt;
    public string updatedAt;
    public string ipAddress;
    public string userAgent;
    public string userId;
    public string id;
}

public class User
{
    public string name;
    public string email;
    public bool emailVerified;
    public string? image;
    public string createdAt;
    public string updatedAt;
    public string firstName;
    public string lastName;
    public string id;
}