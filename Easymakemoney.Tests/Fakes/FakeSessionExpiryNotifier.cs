using Easymakemoney.Services.Auth;

namespace Easymakemoney.Tests.Fakes;

public class FakeSessionExpiryNotifier : ISessionExpiryNotifier
{
    public int NotificationCount { get; private set; }

    public Task NotifySessionExpiredAsync()
    {
        NotificationCount++;
        return Task.CompletedTask;
    }

    public void Reset()
    {
        NotificationCount = 0;
    }
}

