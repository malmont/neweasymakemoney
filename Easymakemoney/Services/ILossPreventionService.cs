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

        // Mode exercice (EX-1) : routes à part, ouvertes même sans le droit du module.
        Task<LossExercises> GetExercisesAsync();
        Task<LossExerciseLaunch> LaunchExerciseAsync(string scenario, bool sendTestEmail);
        Task<LossFindingDetail> GetExerciseAsync(int id);
        Task<LossFindingDetail> DecideExerciseAsync(int id, string status, string? note);
        Task DeleteExercisesAsync();
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

        public Task<LossFindingDetail> DecideAsync(int id, string status, string? note) => Decide($"gestion/pertes/constats/{id}/decision", status, note);

        private async Task<LossFindingDetail> Decide(string path, string status, string? note)
        {
            var body = new Dictionary<string, object?> { ["status"] = status };
            if (!string.IsNullOrWhiteSpace(note))
                body["note"] = note.Trim();
            return await _httpService.PutAsyncWithAuth<LossFindingDetail>($"{path}?locale={Locale}", body) ?? new LossFindingDetail();
        }

        public async Task<LossExercises> GetExercisesAsync() =>
            await _httpService.GetAsync<LossExercises>($"gestion/pertes/exercices?locale={Locale}") ?? new LossExercises();

        public async Task<LossExerciseLaunch> LaunchExerciseAsync(string scenario, bool sendTestEmail) =>
            await _httpService.PostAsyncWithAuth<LossExerciseLaunch>($"gestion/pertes/exercices?locale={Locale}",
                new Dictionary<string, object> { ["scenario"] = scenario, ["sendTestEmail"] = sendTestEmail }) ?? new LossExerciseLaunch();

        public async Task<LossFindingDetail> GetExerciseAsync(int id) =>
            await _httpService.GetAsync<LossFindingDetail>($"gestion/pertes/exercices/{id}?locale={Locale}") ?? new LossFindingDetail();

        public Task<LossFindingDetail> DecideExerciseAsync(int id, string status, string? note) => Decide($"gestion/pertes/exercices/{id}/decision", status, note);

        public async Task DeleteExercisesAsync() => await _httpService.DeleAsyncWithAuth<object>($"gestion/pertes/exercices?locale={Locale}");
    }
}
