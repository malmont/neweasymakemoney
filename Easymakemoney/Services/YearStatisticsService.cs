namespace Easymakemoney.Services
{
    /// <summary>L'exercice de l'entreprise et son réglage (lots backend AN + EX).</summary>
    public class YearStatisticsService : IYearStatisticsService
    {
        private const string Locale = "fr";

        private readonly IHttpService _httpService;

        public YearStatisticsService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public static string YearUrl(int? year, int? pricePercent)
        {
            var parts = new List<string>();
            if (year is { } y)
                parts.Add($"year={y}");
            if (pricePercent is { } p)
                parts.Add($"pricePercent={p}");
            parts.Add($"locale={Locale}");
            return "gestion/statistiques/annee?" + string.Join("&", parts);
        }

        public async Task<YearStatistics> GetAsync(int? year, int? pricePercent) =>
            await _httpService.GetAsync<YearStatistics>(YearUrl(year, pricePercent)) ?? new YearStatistics();

        public async Task<YearComment> GetCommentAsync(int year) =>
            await _httpService.PostAsyncWithAuth<YearComment>($"gestion/statistiques/annee/avis?year={year}&locale={Locale}", new Dictionary<string, object>())
            ?? new YearComment();

        public async Task<FiscalYearSetting> GetFiscalYearAsync() =>
            await _httpService.GetAsync<FiscalYearSetting>("gestion/reglages/exercice") ?? new FiscalYearSetting();

        public async Task<FiscalYearSetting> SaveFiscalYearAsync(int startMonth) =>
            await _httpService.PutAsyncWithAuth<FiscalYearSetting>("gestion/reglages/exercice", new Dictionary<string, object> { ["startMonth"] = startMonth })
            ?? new FiscalYearSetting { startMonth = startMonth };
    }
}
