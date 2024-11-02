namespace Easymakemoney.UseCase.FraisEvolutionUseCase
{
    public class FraisEvolutionUseCase 
    {
        private readonly IFraisEvolutionService _fraisEvolutionService;

        public FraisEvolutionUseCase(IFraisEvolutionService fraisEvolutionService)
        {
            _fraisEvolutionService = fraisEvolutionService;
        }

        public async Task<FraisEvolution> GetFraisEvolutionAsync()
        {
            return await _fraisEvolutionService.GetFraisEvolutionAsync();
        }
    }
}