// Règle de nettoyage
using System.Diagnostics;
using Easymakemoney.Services.Session;
using Easymakemoney.Utilities;

namespace Easymakemoney.Services.Tenant;

public class SiteDataCleaner : ISiteDataCleaner
{
    private readonly ITenantStore _tenantStore;
    private readonly ISessionService _sessionService;
    private readonly Easymakemoney.Services.Offline.IResponseCache _responseCache;

    public SiteDataCleaner(ITenantStore tenantStore, ISessionService sessionService,
        Easymakemoney.Services.Offline.IResponseCache? responseCache = null)
    {
        _tenantStore = tenantStore;
        _sessionService = sessionService;
        _responseCache = responseCache ?? new Easymakemoney.Services.Offline.FileResponseCache();
    }

    /// <summary>
    /// Stockage de l'appareil : SecureStorage (courriel mémorisé compris, règle N6) et préférences (copie de l'Accueil comprise), puis la
    /// marque de premier lancement remise. Point d'entrée des tests : ces API de l'appareil n'existent pas hors de l'application.
    /// </summary>
    public Action ClearDeviceStorage { get; set; } = () =>
    {
        try
        {
            SecureStorage.Default.Remove(SessionKeys.LastLoginEmailKey);
            SecureStorage.Default.RemoveAll();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SiteDataCleaner] Erreur RemoveAll SecureStorage : {ex.Message}");
        }

        Preferences.Default.Clear();
        Preferences.Default.Set(TenantStore.FirstRunKey, true);
    };

    public async Task ClearAllAsync()
    {
        ClearDeviceStorage();
        _responseCache.Clear();
        Easymakemoney.Services.Offline.OfflineState.Online();

        await _sessionService.ClearSessionAsync();
        _tenantStore.ClearMemory();
        SiteCurrency.Reset();
    }
}
