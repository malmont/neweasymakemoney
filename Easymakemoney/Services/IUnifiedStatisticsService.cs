namespace Easymakemoney.Services
{
    public interface IUnifiedStatisticsService
    {
        Task<UnifiedStatistics> GetAsync(StatsQuery query);
    }
}
