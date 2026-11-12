// Code antérieur aux annotations de nullité : avertissements coupés ici, sans effet sur l'exécution.
#nullable disable warnings
namespace Easymakemoney.Services
{

    public class PaiementRemboursementService : IPaiementRemboursementService
    {
        private readonly IHttpService _httpService;

        public PaiementRemboursementService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<PaiementRemboursementEvolution> GetPaiementRemboursementEvolutionAsync()
        {
            var url = "payments/statistics";
            var paiementRemboursementEvolution = await _httpService.GetAsync<PaiementRemboursementEvolution>(url);
            return paiementRemboursementEvolution ?? null;
        }




    }
}