namespace NetAddressWinUI.Service;

public interface IAuthService
{
    Task<NetAddressWinUI.Models.SessionContext?> GetSessionContext();
    Task<bool> ValidateToken();
    Task Logout();
}