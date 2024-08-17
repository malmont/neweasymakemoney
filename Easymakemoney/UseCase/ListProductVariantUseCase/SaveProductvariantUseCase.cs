namespace Easymakemoney.UseCase.ListProductVariantUseCase
{
    public class SaveProductvariantUseCase
    {
        private readonly CreateProductsVariantUseCase _createProductsVariantUseCase;

        public SaveProductvariantUseCase(CreateProductsVariantUseCase createProductsVariantUseCase)
        {
            _createProductsVariantUseCase = createProductsVariantUseCase;
        }

        public async Task<bool> ExecuteAsync(ProductVariantFormModel productVariantFormM, int productId)
        {
            var newProductVariant = new ProductVariant
            {
                
                color = productVariantFormM.SelectedColor,
                size = productVariantFormM.SelectedSize,
                stockQuantity = productVariantFormM.StockQuantity,
            };

            var result = await _createProductsVariantUseCase.ExecuteAsync(newProductVariant, productId);
            return result;
           
           
        }
    }
}