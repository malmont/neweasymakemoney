using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeUnifiedStatisticsService : IUnifiedStatisticsService
{
    public List<StatsQuery> Queries { get; } = new();
    public List<string> RequestedUrls { get; } = new();
    public UnifiedStatistics ResultToReturn { get; set; } = new();
    public Exception? ExceptionToThrow { get; set; }

    public Task<UnifiedStatistics> GetAsync(StatsQuery query)
    {
        Queries.Add(query);
        RequestedUrls.Add(query.Url("fr"));
        if (ExceptionToThrow != null)
            throw ExceptionToThrow;
        return Task.FromResult(ResultToReturn);
    }
}

