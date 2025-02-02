namespace Easymakemoney.Services
{
    public class FraisEvolutionService: IFraisEvolutionService
    {
    private const string FraisEvolutionUrl = Configurations.BackendSymfonyUrl;
     private readonly IHttpService _httpService;
        public FraisEvolutionService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<FraisEvolution> GetFraisEvolutionAsync()
        {
            var url = $"{FraisEvolutionUrl}/frais/total";
            var fraisEvolution = await _httpService.GetAsync<FraisEvolution>(url);
            return fraisEvolution ?? null;
        }
     }
   
}
