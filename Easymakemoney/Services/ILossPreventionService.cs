namespace Easymakemoney.Services
{
    /// <summary>Prévention des pertes (GET /api/gestion/pertes, constats, fiche, décision) ; textes en français.</summary>
    public interface ILossPreventionService
    {
        /// <summary>analyse=false : relire sans lancer d'analyse (rafraîchissement).</summary>
        Task<LossSummary> GetSummaryAsync(bool analyse = true);
        Task<List<LossFinding>> GetFindingsAsync(string? status, bool onlyNew, int page, int limit);
        Task<LossFindingDetail> GetFindingAsync(int id);
        Task<LossFindingDetail> DecideAsync(int id, string status, string? note);
    }

    public class LossPreventionService : ILossPreventionService
    {
        private const string Locale = "fr";
        private readonly IHttpService _httpService;

        public LossPreventionService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<LossSummary> GetSummaryAsync(bool analyse = true) =>
            await _httpService.GetAsync<LossSummary>($"gestion/pertes?{(analyse ? string.Empty : "analyse=0&")}locale={Locale}") ?? new LossSummary();

        public async Task<List<LossFinding>> GetFindingsAsync(string? status, bool onlyNew, int page, int limit)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(status))
                parts.Add($"status={Uri.EscapeDataString(status)}");
            if (onlyNew)
                parts.Add("isNew=1");
            parts.Add("sort=date");
            parts.Add($"page={page}");
            parts.Add($"limit={limit}");
            parts.Add($"locale={Locale}");
            return await _httpService.GetAsync<List<LossFinding>>("gestion/pertes/constats?" + string.Join("&", parts)) ?? new List<LossFinding>();
        }

        public async Task<LossFindingDetail> GetFindingAsync(int id) =>
            await _httpService.GetAsync<LossFindingDetail>($"gestion/pertes/constats/{id}?locale={Locale}") ?? new LossFindingDetail();

        public async Task<LossFindingDetail> DecideAsync(int id, string status, string? note)
        {
            var body = new Dictionary<string, object?> { ["status"] = status };
            if (!string.IsNullOrWhiteSpace(note))
                body["note"] = note.Trim();
            return await _httpService.PutAsyncWithAuth<LossFindingDetail>($"gestion/pertes/constats/{id}/decision?locale={Locale}", body) ?? new LossFindingDetail();
        }
    }
}
