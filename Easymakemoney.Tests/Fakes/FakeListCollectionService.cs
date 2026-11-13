using System.Collections.ObjectModel;
using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeListCollectionService : IListCollectionService
{
    public ObservableCollection<ListCollection> CollectionsToReturn { get; set; } = new();
    public List<int> DeleteCalls { get; } = new();
    public int GetCalls { get; private set; }
    public List<ListCollection> PostCalls { get; } = new();
    public Exception? GetException { get; set; }
    public Exception? PostException { get; set; }
    public Exception? DeleteException { get; set; }
    public bool PostResult { get; set; } = true;
    public bool DeleteResult { get; set; } = true;

    public Task<ObservableCollection<ListCollection>> GetCollectionList()
    {
        GetCalls++;
        if (GetException != null)
            throw GetException;
        return Task.FromResult(CollectionsToReturn);
    }

    public Task<bool> PostCollection(ListCollection newCollection)
    {
        PostCalls.Add(newCollection);
        if (PostException != null)
            throw PostException;
        return Task.FromResult(PostResult);
    }

    public Task<bool> DeleteCollection(int id)
    {
        DeleteCalls.Add(id);
        if (DeleteException != null)
            throw DeleteException;
        return Task.FromResult(DeleteResult);
    }
}
