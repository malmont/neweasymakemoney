namespace Easymakemoney.Services
{
    public interface IPeriodBudgetService
    {
        Task<PeriodBudget> GetAsync(int periodId);
        Task<PeriodBudget> SavePlannedAsync(int periodId, string category, long plannedCents);
        Task<PeriodBudget> CopyAsync(int periodId, int? fromPeriodId = null);
    }
}
