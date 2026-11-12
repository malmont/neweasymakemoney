using System.Collections.ObjectModel;
using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeListCollectionService : IListCollectionService
{
    public ObservableCollection<ListCollection> CollectionsToReturn { get; set; } = new();
    public List<int> DeleteCalls { get; } = new();
    public Exception? GetException { get; set; }
    public Exception? DeleteException { get; set; }
    public bool DeleteResult { get; set; } = true;

    public Task<ObservableCollection<ListCollection>> GetCollectionList()
    {
        if (GetException != null)
            throw GetException;
        return Task.FromResult(CollectionsToReturn);
    }

    public Task<bool> PostCollection(ListCollection newCollection) => Task.FromResult(true);

    public Task<bool> DeleteCollection(int id)
    {
        DeleteCalls.Add(id);
        if (DeleteException != null)
            throw DeleteException;
        return Task.FromResult(DeleteResult);
    }
}
