namespace Easymakemoney.Services
{
    /// <summary>Préparer une période entière (lot backend P6, docs/preparation-periode.md).</summary>
    public interface IPreparationService
    {
        Task<PreparationEstimates> GetEstimatesAsync(string date);
        Task<PreparationSimulation> SimulateAsync(PreparationRequest request);
        Task<PreparationAdvice> AdviseAsync(PreparationRequest request);
        Task ValidateAsync(PreparationRequest request, string previewHash);
    }

    public class PreparationService : IPreparationService
    {
        private readonly IHttpService _httpService;

        public PreparationService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public Task<PreparationEstimates> GetEstimatesAsync(string date) =>
            _httpService.GetAsync<PreparationEstimates>($"gestion/preparation/estimations?date={Uri.EscapeDataString(date)}");

        public Task<PreparationSimulation> SimulateAsync(PreparationRequest request) =>
            _httpService.PostAsyncWithAuth<PreparationSimulation>("gestion/preparation/simulation", request.ToBody());

        public async Task<PreparationAdvice> AdviseAsync(PreparationRequest request) =>
            (await _httpService.PostAsyncWithAuth<PreparationAdvice>("gestion/preparation/avis?locale=fr", request.ToBody())).Prepare();

        public async Task ValidateAsync(PreparationRequest request, string previewHash)
        {
            var body = request.ToBody();
            body["previewHash"] = previewHash;
            await _httpService.PostAsyncWithAuth<object>("gestion/preparation/valider", body);
        }
    }
}
