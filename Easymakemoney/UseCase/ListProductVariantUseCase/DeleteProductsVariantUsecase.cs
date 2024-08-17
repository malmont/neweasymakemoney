namespace Easymakemoney.UseCase.ListProductVariantUseCase
{
    public class DeleteProductsVariantUseCase
    {
        private readonly IListProductVarianstService _listProductVarianstService;

        public DeleteProductsVariantUseCase(IListProductVarianstService listProductVarianstService)
        {
            _listProductVarianstService = listProductVarianstService;
        }

        public async Task<bool> ExecuteAsync(int id)
        {
            return await _listProductVarianstService.DeleteProductVariant(id);
        }
       
    }
}