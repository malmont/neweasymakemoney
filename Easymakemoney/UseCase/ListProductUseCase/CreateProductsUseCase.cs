namespace Easymakemoney.UseCase.ListProductUseCase

{
    public class CreateProductsUseCase
    {
        private readonly IListProductService _listProductService;

        public CreateProductsUseCase(IListProductService listProductService)
        {
            _listProductService = listProductService;
        }

        public async Task<bool> ExecuteAsync(ListProduct newProduct, Stream imageStream, string fileName, int id)
        {
            var result = await _listProductService.PostProducts(newProduct, imageStream, fileName, id);
            if (result)
            {
                Debug.WriteLine("Product created successfully.");
                return true;
            }
            else
            {
                Debug.WriteLine("Failed to create product.");
                return false;
            }
        }
    }
}