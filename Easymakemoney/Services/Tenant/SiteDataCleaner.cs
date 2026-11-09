namespace Easymakemoney.Services.Tenant;

public class SiteDataCleaner : ISiteDataCleaner
{
    private readonly ITenantStore _tenantStore;

    public SiteDataCleaner(ITenantStore tenantStore)
    {
        _tenantStore = tenantStore;
    }

    public Task ClearAllAsync()
    {
        try
        {
            SecureStorage.Default.RemoveAll();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SiteDataCleaner] Erreur RemoveAll SecureStorage : {ex.Message}");
        }

        Preferences.Default.Clear();
        Preferences.Default.Set(TenantStore.FirstRunKey, true);

        App.Token = null;
        App.UserDetails = null;

        _tenantStore.ClearMemory();

        return Task.CompletedTask;
    }

    public async Task<bool> PromptAndChangeSiteAsync()
    {
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Changer de site",
            "Changer de site ? Vous serez déconnecté et les données de ce site seront effacées de l'appareil.",
            "Oui",
            "Non");

        if (!confirm)
        {
            return false;
        }

        await ClearAllAsync();
        await Shell.Current.GoToAsync("//ActivationPage");
        return true;
    }
}
