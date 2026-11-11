namespace Easymakemoney.Services
{
    public interface IManagementModuleService
    {
        Task<FeatureRequestResult> RequestAsync(string feature = ModuleInactiveInfo.ManagementFeature, string? message = null);
        Task<ManagementAi> GetAiAsync();
    }

    /// <summary>
    /// Lot MOD : demande d'essai ou d'activation du module de gestion (route existante du site), unités IA de la gestion.
    /// </summary>
    public class ManagementModuleService : IManagementModuleService
    {
        private readonly IHttpService _httpService;

        public ManagementModuleService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<FeatureRequestResult> RequestAsync(string feature = ModuleInactiveInfo.ManagementFeature, string? message = null)
        {
            var body = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(message))
                body["message"] = message.Trim();
            return await _httpService.PostAsyncWithAuth<FeatureRequestResult>($"site/features/{feature}/request", body) ?? new FeatureRequestResult();
        }

        public async Task<ManagementAi> GetAiAsync() =>
            await _httpService.GetAsync<ManagementAi>("gestion/ia") ?? new ManagementAi();
    }
}
