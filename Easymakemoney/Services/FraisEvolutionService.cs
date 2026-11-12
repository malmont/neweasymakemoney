// Code antérieur aux annotations de nullité : avertissements coupés ici, sans effet sur l'exécution.
#nullable disable warnings
namespace Easymakemoney.Services
{
    public class FraisEvolutionService: IFraisEvolutionService
    {
     private readonly IHttpService _httpService;
        public FraisEvolutionService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<FraisEvolution> GetFraisEvolutionAsync()
        {
            var url = "frais/total";
            var fraisEvolution = await _httpService.GetAsync<FraisEvolution>(url);
            return fraisEvolution ?? null;
        }
     }
   
}
