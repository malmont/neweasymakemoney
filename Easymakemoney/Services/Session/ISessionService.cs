using Easymakemoney.Models.Session;

namespace Easymakemoney.Services.Session;

public interface ISessionService
{
    UserInfo? CurrentUser { get; }
    Task SaveSessionAsync(string token, string refreshToken, UserInfo user);
    Task<string?> GetTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task UpdateTokenAsync(string newToken);
    Task<UserInfo?> GetUserAsync();
    Task<string?> GetLastLoginEmailAsync();
    Task SaveLastLoginEmailAsync(string email);
    Task ClearSessionAsync();
}
