namespace Easymakemoney.UseCase.ListProductVariantUseCase
{
    public class CreateProductsVariantUseCase
    {
        private readonly IListProductVarianstService _listProductVarianstService;

        public CreateProductsVariantUseCase(IListProductVarianstService listProductVarianstService)
        {
            _listProductVarianstService = listProductVarianstService;
        }

        public async Task<bool> ExecuteAsync(ProductVariant newProductVariant, int id)
        {
            return await _listProductVarianstService.PostProductVariant(newProductVariant, id);
        }
    }
}