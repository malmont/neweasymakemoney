using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakePeriodBudgetService : IPeriodBudgetService
{
    public List<int> GetCalls { get; } = new();
    public List<(int PeriodId, string Category, long PlannedCents)> SavePlannedCalls { get; } = new();
    public List<(int PeriodId, int? FromPeriodId)> CopyCalls { get; } = new();
    public List<(int PeriodId, IReadOnlyDictionary<string, long> Lines)> SaveLinesCalls { get; } = new();

    public PeriodBudget BudgetResult { get; set; } = new();
    public Exception? GetException { get; set; }
    public Exception? SavePlannedException { get; set; }
    public Exception? CopyException { get; set; }
    public Exception? SaveLinesException { get; set; }

    public Task<PeriodBudget> GetAsync(int periodId)
    {
        GetCalls.Add(periodId);
        if (GetException != null)
            throw GetException;
        return Task.FromResult(BudgetResult);
    }

    public Task<PeriodBudget> SavePlannedAsync(int periodId, string category, long plannedCents)
    {
        SavePlannedCalls.Add((periodId, category, plannedCents));
        if (SavePlannedException != null)
            throw SavePlannedException;
        return Task.FromResult(BudgetResult);
    }

    public Task<PeriodBudget> CopyAsync(int periodId, int? fromPeriodId = null)
    {
        CopyCalls.Add((periodId, fromPeriodId));
        if (CopyException != null)
            throw CopyException;
        return Task.FromResult(BudgetResult);
    }

    public Task<PeriodBudget> SaveLinesAsync(int periodId, IReadOnlyDictionary<string, long> plannedByCategory)
    {
        SaveLinesCalls.Add((periodId, plannedByCategory));
        if (SaveLinesException != null)
            throw SaveLinesException;
        return Task.FromResult(BudgetResult);
    }
}
