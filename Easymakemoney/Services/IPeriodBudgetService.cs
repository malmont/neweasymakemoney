namespace Easymakemoney.Services
{
    public interface IPeriodBudgetService
    {
        Task<PeriodBudget> GetAsync(int periodId);
        Task<PeriodBudget> SavePlannedAsync(int periodId, string category, long plannedCents);
        Task<PeriodBudget> CopyAsync(int periodId, int? fromPeriodId = null);

        /// <summary>Remplace le prévu de plusieurs postes d'un coup (répartition choisie dans l'avis du guide).</summary>
        Task<PeriodBudget> SaveLinesAsync(int periodId, IReadOnlyDictionary<string, long> plannedByCategory);
    }
}
