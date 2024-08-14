namespace Easymakemoney.UseCase.ListCategoryUseCase
{
    public class GetListCategoryUseCase
    {
        private readonly IListCategoryServices _listCategoryServices;

        public GetListCategoryUseCase(IListCategoryServices listCategoryServices)
        {
            _listCategoryServices = listCategoryServices;
        }

        public async Task<ObservableCollection<Category>> ExecuteAsync()
        {
            var categories = await _listCategoryServices.GetCategoryList();
            return categories;
        }
    }
}