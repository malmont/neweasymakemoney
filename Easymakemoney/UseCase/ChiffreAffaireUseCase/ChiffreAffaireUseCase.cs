namespace Easymakemoney.UseCase.ChiffreAffaireUseCase
{
    public class ChiffreAffaireUseCase 
    {
        private readonly IChiffreAffaireService _chiffreAffaireService;

        public ChiffreAffaireUseCase(IChiffreAffaireService chiffreAffaireService)
        {
            _chiffreAffaireService = chiffreAffaireService;
        }

        public async Task<RevenueStatistics> GetRevenueStatistics()
        {
            return await _chiffreAffaireService.GetRevenueStatistics();
        }
    }
}