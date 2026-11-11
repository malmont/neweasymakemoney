namespace Easymakemoney.Services
{
    public interface IRentalStatisticsService
    {
        Task<RentalStatistics> GetAsync(RentalQuery query);
    }

    /// <summary>Statistiques des locations (lot backend LOC1).</summary>
    public class RentalStatisticsService : IRentalStatisticsService
    {
        private readonly IHttpService _httpService;

        public RentalStatisticsService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<RentalStatistics> GetAsync(RentalQuery query) =>
            await _httpService.GetAsync<RentalStatistics>(query.Url("fr")) ?? new RentalStatistics();
    }
}
