namespace Easymakemoney.Services
{
    public class DashBoardCollectionService : IDashBoardCollectionService
    {
        private readonly IHttpService _httpService;

        public DashBoardCollectionService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<DashBoardCollection> GetDashBoardCollection(int CollectionId)
        {
            var url = $"dashboard/collection/{CollectionId}";
            var dashBoardCollection = await _httpService.GetAsync<DashBoardCollection>(url);
            return dashBoardCollection ?? null;
        }

       
    }
   
}