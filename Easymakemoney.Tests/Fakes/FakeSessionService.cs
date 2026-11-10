using Easymakemoney.Models.Session;
using Easymakemoney.Services.Session;

namespace Easymakemoney.Tests.Fakes;

public class FakeSessionService : ISessionService
{
    public UserInfo? CurrentUser { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public string? LastLoginEmail { get; set; }

    public Task SaveSessionAsync(string token, string refreshToken, UserInfo user)
    {
        Token = token;
        RefreshToken = refreshToken;
        CurrentUser = user;
        return Task.CompletedTask;
    }

    public Task<string?> GetTokenAsync() => Task.FromResult(Token);

    public Task<string?> GetRefreshTokenAsync() => Task.FromResult(RefreshToken);

    public Task UpdateTokenAsync(string newToken)
    {
        Token = newToken;
        return Task.CompletedTask;
    }

    public Task<UserInfo?> GetUserAsync() => Task.FromResult(CurrentUser);

    public Task<string?> GetLastLoginEmailAsync() => Task.FromResult(LastLoginEmail);

    public Task SaveLastLoginEmailAsync(string email)
    {
        LastLoginEmail = email;
        return Task.CompletedTask;
    }

    public Task ClearSessionAsync()
    {
        Token = null;
        RefreshToken = null;
        CurrentUser = null;
        return Task.CompletedTask;
    }
}

