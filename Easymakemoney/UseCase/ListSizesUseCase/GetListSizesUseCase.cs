namespace Easymakemoney.UseCase.ListSizesUseCase
{
    public class GetListSizesUseCase 
    {
        private readonly IListSizesServices _listSizesServices;

        public GetListSizesUseCase(IListSizesServices listSizesServices)
        {
            _listSizesServices = listSizesServices;
        }

        public async Task<ObservableCollection<SizeProduct>> GetListSizes()
        {
            return await _listSizesServices.GetSizesList();
        }
    }
}