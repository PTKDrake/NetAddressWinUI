namespace NetAddressWinUI.Models;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SocialAuthContent
{
    public string Provider { get; set; } = "google";
    public IdToken IdToken { get; set; } = new();
}

public class IdToken
{
    public string Token { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
}

public class AuthFailContent
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class SessionContext
{
    public bool Redirect { get; set; }
    public string Token { get; set; } = string.Empty;
    public User User { get; set; } = new();
}

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string Token { get; set; } = string.Empty;
} 