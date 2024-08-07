namespace Easymakemoney.Services
{
    public class ListCollectionService : IListCollectionService
    {
        private const string CollectionsUrl = "https://backend-strapi.online/jeesign/api";
        private readonly IHttpService _httpService;

        public ListCollectionService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<ObservableCollection<ListCollection>> GetCollectionList()
        {
            var collections = await _httpService.GetAsync<ObservableCollection<ListCollection>>(CollectionsUrl +"/collections.json");
            return collections ?? new ObservableCollection<ListCollection>();
        }

        public async Task<bool> PostCollection(ListCollection newCollection)
        {
            var result = await _httpService.PostAsyncWithAuth<object>(CollectionsUrl + "/createcollections", newCollection);
            return result != null;
        }
    }
}