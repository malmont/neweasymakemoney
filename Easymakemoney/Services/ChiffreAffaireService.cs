// Code antérieur aux annotations de nullité : avertissements coupés ici, sans effet sur l'exécution.
#nullable disable warnings
namespace Easymakemoney.Services
{
    public class ChiffreAffaireService : IChiffreAffaireService
    {
        private readonly IHttpService _httpService;

        public ChiffreAffaireService(IHttpService httpService)
        {
            _httpService = httpService;
        }
        public async Task<RevenueStatistics> GetRevenueStatistics()
        {
            var url = "statistiques/chiffre-affaires";
            var revenueStatistics = await _httpService.GetAsync<RevenueStatistics>(url);
            return revenueStatistics ?? null;
        }

       
    }
}