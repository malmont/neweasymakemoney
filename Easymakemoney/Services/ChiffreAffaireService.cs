namespace Easymakemoney.Services
{
    public class ChiffreAffaireService : IChiffreAffaireService
    {

         private const string RevenueCollectionsUrl = "https://backend-strapi.online/jeesign/api";
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