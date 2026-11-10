namespace Easymakemoney.Services
{
    public interface IPeriodService
    {
        Task<ListCollection?> GetCurrentPeriodAsync();
        Task UpdateBudgetAsync(int periodId, long budgetCents);
    }
}
