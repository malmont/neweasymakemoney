using System.Collections.ObjectModel;
using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeListCollectionService : IListCollectionService
{
    public ObservableCollection<ListCollection> CollectionsToReturn { get; set; } = new();

    public Task<ObservableCollection<ListCollection>> GetCollectionList() =>
        Task.FromResult(CollectionsToReturn);

    public Task<bool> PostCollection(ListCollection newCollection) => Task.FromResult(true);

    public Task<bool> DeleteCollection(int id) => Task.FromResult(true);
}

