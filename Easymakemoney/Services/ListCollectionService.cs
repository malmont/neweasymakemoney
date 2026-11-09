namespace Easymakemoney.Services
{
    public class ListCollectionService : IListCollectionService
    {
        private readonly IHttpService _httpService;

        public ListCollectionService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<ObservableCollection<ListCollection>> GetCollectionList()
        {
            var collections = await _httpService.GetAsync<ObservableCollection<ListCollection>>("collections");
            return collections ?? new ObservableCollection<ListCollection>();
        }

        public async Task<bool> PostCollection(ListCollection newCollection)
        {
            await _httpService.PostAsyncWithAuth<object>("createcollections", newCollection);
            return true;
        }

        public async Task<bool> DeleteCollection(int id)
        {
            var url = $"collections/{id}";
            var result = await _httpService.DeleAsyncWithAuth<object>(url);
            return result;
        }
    }
}