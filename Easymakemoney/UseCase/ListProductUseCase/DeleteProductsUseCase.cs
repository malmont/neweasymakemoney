namespace Easymakemoney.UseCase.ListProductUseCase
{
    public class DeleteProductsUseCase
    {
        private readonly IListProductService _listProductService;

        public DeleteProductsUseCase(IListProductService listProductService)
        {
            _listProductService = listProductService;
        }

        public async Task<bool> ExecuteAsync(int id)
        {
            var result = await _listProductService.DeleteProducts(id);
            if (result)
            {
                Debug.WriteLine("Product deleted successfully.");
                return true;
            }
            else
            {
                Debug.WriteLine("Failed to delete product.");
                return false;
            }
        }
    }
}