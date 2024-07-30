namespace Easymakemoney.UseCases
{
    public class GetListCollectionsUseCase
    {
        private readonly IListCollectionService _listCollectionService;

        public GetListCollectionsUseCase(IListCollectionService listCollectionService)
        {
            _listCollectionService = listCollectionService;
        }

        public async Task<ObservableCollection<ListCollection>> ExecuteAsync()
        {
            return await _listCollectionService.GetCollectionList();
        }
    }
}
