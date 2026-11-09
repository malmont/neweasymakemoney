// Règle de nettoyage
using System.Diagnostics;
using Easymakemoney.Services.Session;

namespace Easymakemoney.Services.Tenant;

public class SiteDataCleaner : ISiteDataCleaner
{
    private readonly ITenantStore _tenantStore;
    private readonly ISessionService _sessionService;

    public SiteDataCleaner(ITenantStore tenantStore, ISessionService sessionService)
    {
        _tenantStore = tenantStore;
        _sessionService = sessionService;
    }

    public async Task ClearAllAsync()
    {
        try
        {
            // Règle N6 : ClearAllAsync (changement de site) efface aussi le courriel mémorisé
            SecureStorage.Default.Remove(SessionKeys.LastLoginEmailKey);
            SecureStorage.Default.RemoveAll();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SiteDataCleaner] Erreur RemoveAll SecureStorage : {ex.Message}");
        }

        Preferences.Default.Clear();
        Preferences.Default.Set(TenantStore.FirstRunKey, true);

        await _sessionService.ClearSessionAsync();
        _tenantStore.ClearMemory();
    }
}
