namespace Easymakemoney.Services
{
    public class CollectionCloseService : ICollectionCloseService
    {
        private const string CollectionsCloseUrl = Configurations.BackendSymfonyUrl;
        private readonly IHttpService _httpService;

        public CollectionCloseService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<string> GetCollectionClose(int CollectionId)
        {
            var url = $"{CollectionsCloseUrl}/dashboard/collection/{CollectionId}/close";
            var collectionClose = await _httpService.PostAsyncWithAuth<string>(url,null);
            return collectionClose ?? null;
        }
    }


}