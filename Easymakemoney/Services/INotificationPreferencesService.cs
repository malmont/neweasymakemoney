namespace Easymakemoney.Services
{
    /// <summary>Préférences de notification de la gestion (niveau des courriels de suivi, types « Avancé ») ; textes en français.</summary>
    public interface INotificationPreferencesService
    {
        Task<NotificationPreferences> GetAsync();

        /// <summary>level null : « Comme le site ».</summary>
        Task<NotificationPreferences> SetLevelAsync(string? level);
        Task<NotificationPreferences> SetKindAsync(string kind, bool enabled);
    }

    public class NotificationPreferencesService : INotificationPreferencesService
    {
        private const string Url = "gestion/notifications/preferences?locale=fr";
        private readonly IHttpService _httpService;

        public NotificationPreferencesService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<NotificationPreferences> GetAsync() =>
            await _httpService.GetAsync<NotificationPreferences>(Url) ?? new NotificationPreferences();

        public async Task<NotificationPreferences> SetLevelAsync(string? level) =>
            await _httpService.PatchAsyncWithAuth<NotificationPreferences>(Url, new Dictionary<string, object?> { ["level"] = level }) ?? new NotificationPreferences();

        public async Task<NotificationPreferences> SetKindAsync(string kind, bool enabled) =>
            await _httpService.PatchAsyncWithAuth<NotificationPreferences>(Url,
                new Dictionary<string, object> { ["kinds"] = new Dictionary<string, bool> { [kind] = enabled } }) ?? new NotificationPreferences();
    }
}
