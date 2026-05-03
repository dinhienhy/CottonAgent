using CBAS.Web.Models;

namespace CBAS.Web.Services;

public interface IAuthService
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task<bool> CreateUserAsync(string username, string password, string? fullName = null, string? email = null);
    Task<User?> GetCurrentUserAsync();
    Task LogoutAsync();
}
