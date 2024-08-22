namespace Easymakemoney.Services
{
    public class DashBoardCollectionService : IDashBoardCollectionService
    {
        private const string DashBoardCollectionsUrl = "https://backend-strapi.online/jeesign/api";
        private readonly IHttpService _httpService;

        public DashBoardCollectionService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<DashBoardCollection> GetDashBoardCollection(int CollectionId)
        {
            var url = $"{DashBoardCollectionsUrl}/dashboard/collection/{CollectionId}";
            var dashBoardCollection = await _httpService.GetAsync<DashBoardCollection>(url);
            return dashBoardCollection ?? null;
        }

       
    }
   
}