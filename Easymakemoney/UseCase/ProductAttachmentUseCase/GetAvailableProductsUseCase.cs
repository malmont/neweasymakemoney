namespace Easymakemoney.UseCase.ProductAttachmentUseCase
{
    public class GetAvailableProductsUseCase
    {
        private readonly IProductAttachmentService _service;

        public GetAvailableProductsUseCase(IProductAttachmentService service)
        {
            _service = service;
        }

        public Task<AvailableProductsPage> ExecuteAsync(string? query, int page, int limit, bool allProducts = false) =>
            _service.GetAvailableAsync(query, page, limit, allProducts);
    }
}
