namespace Easymakemoney.UseCase.ListProductUseCase
{
    public class GetListProductsUseCase 
    {
        private readonly IListProductService _listProductService;

        public GetListProductsUseCase(IListProductService listProductService)
        {
            _listProductService = listProductService;
        }

        public async Task<ObservableCollection<ListProduct>> ExecuteAsync(int id)
        {
            return await _listProductService.GetProductsList(id);
        }
    }
}