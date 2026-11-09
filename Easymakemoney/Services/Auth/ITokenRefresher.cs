namespace Easymakemoney.Services.Auth;

public interface ITokenRefresher
{
    Task<RefreshResult> RefreshAsync(string? originalToken, CancellationToken cancellationToken = default);
}

