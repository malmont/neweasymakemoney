namespace Easymakemoney.Services
{
    public class ChiffreAffaireService : IChiffreAffaireService
    {

         private const string RevenueCollectionsUrl = Configurations.BackendSymfonyUrl;
        private readonly IHttpService _httpService;

        public ChiffreAffaireService(IHttpService httpService)
        {
            _httpService = httpService;
        }
        public async Task<RevenueStatistics> GetRevenueStatistics()
        {
            var url = $"{RevenueCollectionsUrl}/statistiques/chiffre-affaires";
            var revenueStatistics = await _httpService.GetAsync<RevenueStatistics>(url);
            return revenueStatistics ?? null;
        }

       
    }
}