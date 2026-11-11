using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeRentalStatisticsService : IRentalStatisticsService
{
    public List<RentalQuery> Queries { get; } = new();
    public List<string> RequestedUrls { get; } = new();
    public RentalStatistics ResultToReturn { get; set; } = new();
    public Exception? ExceptionToThrow { get; set; }

    public Task<RentalStatistics> GetAsync(RentalQuery query)
    {
        Queries.Add(query);
        RequestedUrls.Add(query.Url("fr"));
        if (ExceptionToThrow != null)
            throw ExceptionToThrow;
        return Task.FromResult(ResultToReturn);
    }
}

