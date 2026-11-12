using System.Collections.ObjectModel;
using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeListProductsService : IListProductService
{
    public ObservableCollection<ListProduct> Products { get; set; } = new();
    public List<int> GetCalls { get; } = new();
    public List<int> DeleteCalls { get; } = new();

    public Exception? GetException { get; set; }
    public Exception? DeleteException { get; set; }
    public bool DeleteResult { get; set; } = true;

    public Task<ObservableCollection<ListProduct>> GetProductsList(int id)
    {
        GetCalls.Add(id);
        if (GetException != null)
            throw GetException;
        return Task.FromResult(Products);
    }

    public Task<bool> PostProducts(ListProduct newProduct, Stream imageStream, string fileName, int id)
    {
        return Task.FromResult(true);
    }

    public Task<bool> DeleteProducts(int id)
    {
        DeleteCalls.Add(id);
        if (DeleteException != null)
            throw DeleteException;
        return Task.FromResult(DeleteResult);
    }
}

