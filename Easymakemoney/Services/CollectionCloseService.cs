namespace Easymakemoney.Services
{
    public class CollectionCloseService : ICollectionCloseService
    {
        private readonly IHttpService _httpService;

        public CollectionCloseService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<bool> CloseCollectionAsync(int collectionId)
        {
            var url = $"dashboard/collection/{collectionId}/close";
            await _httpService.PostAsyncWithAuth<object>(url, null);
            return true;
        }
    }


}