using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakePeriodService : IPeriodService
{
    public ListCollection? CurrentPeriodResult { get; set; }
    public Exception? GetCurrentPeriodException { get; set; }
    public List<(int PeriodId, long BudgetCents)> UpdateBudgetCalls { get; } = new();

    public Task<ListCollection?> GetCurrentPeriodAsync()
    {
        if (GetCurrentPeriodException != null)
            throw GetCurrentPeriodException;
        return Task.FromResult(CurrentPeriodResult);
    }

    public Task UpdateBudgetAsync(int periodId, long budgetCents)
    {
        UpdateBudgetCalls.Add((periodId, budgetCents));
        return Task.CompletedTask;
    }
}
