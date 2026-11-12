using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Faux service de clôture et réouverture de période/collection.</summary>
public class FakeCollectionCloseService : ICollectionCloseService
{
    public List<int> CloseCalls { get; } = new();
    public List<int> ReopenCalls { get; } = new();

    public Exception? CloseException { get; set; }
    public Exception? ReopenException { get; set; }

    public Task<bool> CloseCollectionAsync(int collectionId)
    {
        CloseCalls.Add(collectionId);
        if (CloseException != null)
            throw CloseException;
        return Task.FromResult(true);
    }

    public Task<bool> ReopenCollectionAsync(int collectionId)
    {
        ReopenCalls.Add(collectionId);
        if (ReopenException != null)
            throw ReopenException;
        return Task.FromResult(true);
    }
}

