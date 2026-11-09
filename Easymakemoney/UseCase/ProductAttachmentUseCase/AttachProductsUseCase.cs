namespace Easymakemoney.UseCase.ProductAttachmentUseCase
{
    public class AttachProductsUseCase
    {
        private readonly IProductAttachmentService _service;

        public AttachProductsUseCase(IProductAttachmentService service)
        {
            _service = service;
        }

        public Task ExecuteAsync(int commandId, IEnumerable<AvailableProduct> products) =>
            _service.AttachAsync(commandId, products);
    }
}
