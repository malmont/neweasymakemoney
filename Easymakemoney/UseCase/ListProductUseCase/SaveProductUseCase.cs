namespace Easymakemoney.UseCase.ListProductUseCase
{
    public class SaveProductUseCase
    {
        private readonly CreateProductsUseCase _createProductsUseCase;
        

        public SaveProductUseCase(CreateProductsUseCase createProductsUseCase)
        {
            _createProductsUseCase = createProductsUseCase;
        }

        public async Task<bool> ExecuteAsync(ProductFormModel productForm, int CommandId)
        {
            if (productForm.ImageStream == null || string.IsNullOrEmpty(productForm.ImageFileName))
            {
                throw new InvalidOperationException("Image is required.");
            }

            var result = await _createProductsUseCase.ExecuteAsync(productForm.ToListProduct(), productForm.ImageStream, productForm.ImageFileName,CommandId);
       
            return result;
        }
    }
}