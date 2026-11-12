using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeProductAttachmentService : IProductAttachmentService
{
    public List<(string? Query, int Page, int Limit, bool AllProducts)> GetAvailableCalls { get; } = new();
    public List<(int CommandId, List<AvailableProduct> Products)> AttachCalls { get; } = new();
    public List<(int CommandId, int ProductId)> DetachCalls { get; } = new();

    public AvailableProductsPage AvailableProductsResult { get; set; } = new();
    public Exception? GetAvailableException { get; set; }
    public Exception? AttachException { get; set; }
    public Exception? DetachException { get; set; }
    public bool DetachResult { get; set; } = true;

    public Task<AvailableProductsPage> GetAvailableAsync(string? query, int page, int limit, bool allProducts = false)
    {
        GetAvailableCalls.Add((query, page, limit, allProducts));
        if (GetAvailableException != null)
            throw GetAvailableException;
        return Task.FromResult(AvailableProductsResult);
    }

    public Task AttachAsync(int commandId, IEnumerable<AvailableProduct> products)
    {
        AttachCalls.Add((commandId, products.ToList()));
        if (AttachException != null)
            throw AttachException;
        return Task.CompletedTask;
    }

    public Task<bool> DetachAsync(int commandId, int productId)
    {
        DetachCalls.Add((commandId, productId));
        if (DetachException != null)
            throw DetachException;
        return Task.FromResult(DetachResult);
    }
}
