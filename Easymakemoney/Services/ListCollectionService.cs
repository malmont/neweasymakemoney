namespace Easymakemoney.Services
{
    public class ListCollectionService : IListCollectionService
    {
        private const string GetListUrl = "https://backend-strapi.online/jeesign/api/collections.json";
        private readonly IHttpService _httpService;

        public ListCollectionService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<ObservableCollection<ListCollection>> GetCollectionList()
        {
            var collections = await _httpService.GetAsync<ObservableCollection<ListCollection>>(GetListUrl);
            return collections ?? new ObservableCollection<ListCollection>();
        }
    }
}
