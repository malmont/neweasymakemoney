namespace Easymakemoney.Services.Auth;

public interface ISessionExpiryNotifier
{
    Task NotifySessionExpiredAsync();
    void Reset();
}

