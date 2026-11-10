using Easymakemoney.Services.Auth;

namespace Easymakemoney.Tests.Fakes;

public class FakeTokenRefresher : ITokenRefresher
{
    private readonly Func<string?, CancellationToken, Task<RefreshResult>> _refreshFunc;
    public int CallCount { get; private set; }

    public FakeTokenRefresher(Func<string?, CancellationToken, Task<RefreshResult>> refreshFunc)
    {
        _refreshFunc = refreshFunc;
    }

    public FakeTokenRefresher(RefreshResult staticResult)
    {
        _refreshFunc = (_, _) => Task.FromResult(staticResult);
    }

    public async Task<RefreshResult> RefreshAsync(string? originalToken, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        return await _refreshFunc(originalToken, cancellationToken);
    }

    private int _callCount;
    public int InvocationCount => _callCount;
}

