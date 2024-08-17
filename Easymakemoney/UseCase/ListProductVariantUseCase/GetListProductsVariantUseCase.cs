namespace Easymakemoney.UseCase.ListProductVariantUseCase
{
    public class GetListProductsVariantUseCase 
    {
        private readonly IListProductVarianstService _listProductVarianstService;

        public GetListProductsVariantUseCase(IListProductVarianstService listProductVarianstService)
        {
            _listProductVarianstService = listProductVarianstService;
        }

        public async Task<ObservableCollection<ProductVariant>> ExecuteAsync(int id)
        {
            return await _listProductVarianstService.GetProductVariantList(id);
        }
    }
}