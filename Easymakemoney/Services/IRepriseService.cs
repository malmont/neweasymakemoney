namespace Easymakemoney.Services
{
    public interface IRepriseService
    {
        Task<RepriseState> GetStateAsync();
        Task<RepriseSimulation> SimulateAsync(int months, double? coefficient, int page, int limit);
        Task ApplyAsync(int months, double? coefficient, IEnumerable<RepriseProduct> editedProducts);
        Task CancelAsync();
    }
}
