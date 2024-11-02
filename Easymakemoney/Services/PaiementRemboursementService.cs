namespace Easymakemoney.Services
{

    public class PaiementRemboursementService : IPaiementRemboursementService
    {
        private const string PaiementRemboursementUrl = "https://backend-strapi.online/jeesign/api";
        private readonly IHttpService _httpService;

        public PaiementRemboursementService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<PaiementRemboursementEvolution> GetPaiementRemboursementEvolutionAsync()
        {
            var url = $"{PaiementRemboursementUrl}/payments/statistics";
            var paiementRemboursementEvolution = await _httpService.GetAsync<PaiementRemboursementEvolution>(url);
            return paiementRemboursementEvolution ?? null;
        }




    }
}