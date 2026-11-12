using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeProductAttachmentService : IProductAttachmentService
{
    public List<(string? Query, int Page, int Limit, bool AllProducts)> GetAvailableCalls { get; } = new();

    public AvailableProductsPage AvailableProductsResult { get; set; } = new();
    public Exception? GetAvailableException { get; set; }

    public Task<AvailableProductsPage> GetAvailableAsync(string? query, int page, int limit, bool allProducts = false)
    {
        GetAvailableCalls.Add((query, page, limit, allProducts));
        if (GetAvailableException != null)
            throw GetAvailableException;
        return Task.FromResult(AvailableProductsResult);
    }

    public Task AttachAsync(int commandId, IEnumerable<AvailableProduct> products) => throw new NotImplementedException();
    public Task<bool> DetachAsync(int commandId, int productId) => throw new NotImplementedException();
}

