namespace Easymakemoney.UseCase.PaiementRemboursementUseCase
{
    public class PaiementRemboursementUseCase 
    {
        private readonly IPaiementRemboursementService _paiementRemboursementService;

        public PaiementRemboursementUseCase(IPaiementRemboursementService paiementRemboursementService)
        {
            _paiementRemboursementService = paiementRemboursementService;
        }

        public async Task<PaiementRemboursementEvolution> GetPaiementRemboursementEvolutionAsync()
        {
            return await _paiementRemboursementService.GetPaiementRemboursementEvolutionAsync();
        }
    }
}