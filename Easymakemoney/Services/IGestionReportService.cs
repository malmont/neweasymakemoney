using Easymakemoney.Services.Api;

namespace Easymakemoney.Services
{
    /// <summary>Rapports PDF de la gestion (backend DOC-1 et DOC-2) : demander un bilan, suivre sa fabrication, télécharger, lister, effacer.</summary>
    public interface IGestionReportService
    {
        Task<GestionReport> RequestPeriodAsync(int periodId, bool withAdvice);
        /// <summary>year null : l'exercice en cours.</summary>
        Task<GestionReport> RequestYearAsync(int? year, bool withAdvice);
        Task<GestionReport> GetAsync(int id);
        Task<GestionReportList> ListAsync(int page);
        Task<DownloadedFile> DownloadAsync(int id);
        Task DeleteAsync(int id);
    }

    public class GestionReportService : IGestionReportService
    {
        private const string Base = "gestion/rapports";
        private const string Locale = "fr";
        private readonly IHttpService _httpService;

        public GestionReportService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public Task<GestionReport> RequestPeriodAsync(int periodId, bool withAdvice) => Request(new Dictionary<string, object>
        {
            ["kind"] = "period", ["periodId"] = periodId, ["withAdvice"] = withAdvice, ["locale"] = Locale,
        });

        public Task<GestionReport> RequestYearAsync(int? year, bool withAdvice)
        {
            var body = new Dictionary<string, object> { ["kind"] = "year", ["withAdvice"] = withAdvice, ["locale"] = Locale };
            if (year is { } y)
                body["year"] = y;
            return Request(body);
        }

        private async Task<GestionReport> Request(Dictionary<string, object> body) =>
            await _httpService.PostAsyncWithAuth<GestionReport>($"{Base}?locale={Locale}", body) ?? new GestionReport();

        public async Task<GestionReport> GetAsync(int id) =>
            await _httpService.GetAsync<GestionReport>($"{Base}/{id}?locale={Locale}") ?? new GestionReport { id = id };

        public async Task<GestionReportList> ListAsync(int page) =>
            await _httpService.GetAsync<GestionReportList>($"{Base}?page={page}&locale={Locale}") ?? new GestionReportList();

        public Task<DownloadedFile> DownloadAsync(int id) => _httpService.GetFileAsync($"{Base}/{id}/fichier?locale={Locale}");

        public async Task DeleteAsync(int id) => await _httpService.DeleAsyncWithAuth<object>($"{Base}/{id}?locale={Locale}");
    }
}
