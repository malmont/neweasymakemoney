namespace Easymakemoney.UseCase.ProductAttachmentUseCase
{
    public class DetachProductUseCase
    {
        private readonly IProductAttachmentService _service;

        public DetachProductUseCase(IProductAttachmentService service)
        {
            _service = service;
        }

        public Task<bool> ExecuteAsync(int commandId, int productId) =>
            _service.DetachAsync(commandId, productId);
    }
}
