namespace Easymakemoney.Services
{
    /// <summary>Statistiques unifiées (lot backend S1) : libellés et remarques en français.</summary>
    public class UnifiedStatisticsService : IUnifiedStatisticsService
    {
        private const string Locale = "fr";

        private readonly IHttpService _httpService;

        public UnifiedStatisticsService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<UnifiedStatistics> GetAsync(StatsQuery query) =>
            await _httpService.GetAsync<UnifiedStatistics>(query.Url(Locale)) ?? new UnifiedStatistics();
    }
}
