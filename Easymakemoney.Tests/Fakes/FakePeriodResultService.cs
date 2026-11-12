using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Faux service de résultat financier d'une période.</summary>
public class FakePeriodResultService : IPeriodResultService
{
    public List<int> RequestedPeriodIds { get; } = new();
    public PeriodResult ResultToReturn { get; set; } = new();
    public Exception? ExceptionToThrow { get; set; }

    public Task<PeriodResult> GetResultAsync(int periodId)
    {
        RequestedPeriodIds.Add(periodId);
        if (ExceptionToThrow != null)
            throw ExceptionToThrow;
        return Task.FromResult(ResultToReturn);
    }
}

