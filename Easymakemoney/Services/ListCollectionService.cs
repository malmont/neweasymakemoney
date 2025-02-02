namespace Easymakemoney.Services
{
    public class ListCollectionService : IListCollectionService
    {
        private const string CollectionsUrl = Configurations.BackendSymfonyUrl;
        private readonly IHttpService _httpService;

        public ListCollectionService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<ObservableCollection<ListCollection>> GetCollectionList()
        {
            var collections = await _httpService.GetAsync<ObservableCollection<ListCollection>>(CollectionsUrl +"/collections");
            return collections ?? new ObservableCollection<ListCollection>();
        }

        public async Task<bool> PostCollection(ListCollection newCollection)
        {
            var result = await _httpService.PostAsyncWithAuth<object>(CollectionsUrl + "/createcollections", newCollection);
            return result != null;
        }

        public async Task<bool> DeleteCollection(int id)
        {
            var url = $"{CollectionsUrl}/collections/{id}";
            var result = await _httpService.DeleAsyncWithAuth<object>(url);
            return result != null;
        }
    }
}