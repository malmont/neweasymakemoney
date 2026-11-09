namespace Easymakemoney.Services
{
    public interface IProductAttachmentService
    {
        Task<AvailableProductsPage> GetAvailableAsync(string? query, int page, int limit);
        Task AttachAsync(int commandId, IEnumerable<AvailableProduct> products);
        Task<bool> DetachAsync(int commandId, int productId);
    }
}
