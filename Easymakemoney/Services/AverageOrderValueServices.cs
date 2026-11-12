// Code antérieur aux annotations de nullité : avertissements coupés ici, sans effet sur l'exécution.
#nullable disable warnings
namespace Easymakemoney.Models

{
    public class AverageOrderValueServices : IAverageOrderValueServices
    {
        private readonly IHttpService _httpService;

        public AverageOrderValueServices(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<AverageOrderValueStatistics> GetAverageOrderValueStatistics()
        {
            var url = "statistiques/panier-moyen";
            var averageOrderValueStatistics = await _httpService.GetAsync<AverageOrderValueStatistics>(url);
            return averageOrderValueStatistics ?? null;
        }

       
    }
}