namespace Easymakemoney.Services
{
    public interface IPaiementRemboursementService
    {
        Task<PaiementRemboursementEvolution> GetPaiementRemboursementEvolutionAsync();
    }
}