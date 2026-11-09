using Easymakemoney.Models.Tenant;
using Easymakemoney.Services.Site;
using Easymakemoney.Services.Tenant;

namespace Easymakemoney.ViewModels.Startup
{
    public class LoadingPageViewModel
    {
        private readonly ITenantStore _tenantStore;
        private readonly ISiteService _siteService;
        private readonly ISiteDataCleaner _siteDataCleaner;

        public LoadingPageViewModel(
            ITenantStore tenantStore,
            ISiteService siteService,
            ISiteDataCleaner siteDataCleaner)
        {
            _tenantStore = tenantStore;
            _siteService = siteService;
            _siteDataCleaner = siteDataCleaner;

            InitStartup();
        }

        private async void InitStartup()
        {
            try
            {
                // Règle 6.1 : effacement au premier lancement
                await _tenantStore.EnsureFirstRunCleanAsync();

                // Règle 6.2 : lecture du site enregistré
                var tenant = await _tenantStore.GetTenantAsync();
                if (tenant == null)
                {
                    await Shell.Current.GoToAsync("//ActivationPage");
                    return;
                }

                // Règle 6.3 & L9 : revérification en arrière-plan sans retarder l'affichage
                _ = Task.Run(async () => await RecheckTenantInBackgroundAsync(tenant));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadingPageViewModel] Erreur initialisation startup : {ex.Message}");
                await Shell.Current.GoToAsync("//ActivationPage");
                return;
            }

            // Poursuite de la logique actuelle de vérification de session
            checkUserLoginDetails();
        }

        private async Task RecheckTenantInBackgroundAsync(TenantContext tenant)
        {
            try
            {
                var result = await _siteService.CheckSiteCodeAsync(tenant.Code);

                if (result.IsSuccess && result.Tenant != null)
                {
                    if (string.Equals(result.Tenant.Code, tenant.Code, StringComparison.OrdinalIgnoreCase))
                    {
                        // 200 avec le même code : mise à jour de Host et Name s'ils ont changé
                        if (!string.Equals(result.Tenant.Host, tenant.Host, StringComparison.Ordinal) ||
                            !string.Equals(result.Tenant.Name, tenant.Name, StringComparison.Ordinal))
                        {
                            await _tenantStore.SaveTenantAsync(result.Tenant);
                        }
                    }
                    else
                    {
                        // 200 avec un autre code : on ne change rien, consigné dans le journal
                        Debug.WriteLine($"[LoadingPageViewModel] Revérification : code différent reçu ({result.Tenant.Code} vs {tenant.Code}), ignoré.");
                    }
                }
                else if (result.ApiError?.Code == "TENANT_NOT_FOUND")
                {
                    // 404 : effacement complet et retour à l'activation sur le fil principal (L7 & L9)
                    await _siteDataCleaner.ClearAllAsync();

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Shell.Current.DisplayAlertAsync("Information", "Ce site n'est plus disponible.", "OK");
                        await Shell.Current.GoToAsync("//ActivationPage");
                    });
                }
                // Réseau, 429 ou 5xx : on garde le site sans message
            }
            catch (Exception ex)
            {
                // Règle L9 : toute exception attrapée dans la tâche
                Debug.WriteLine($"[LoadingPageViewModel] Erreur revérification en arrière-plan : {ex.Message}");
            }
        }

        private async void checkUserLoginDetails()
        {
            string userDetailsStr = Microsoft.Maui.Storage.Preferences.Get(nameof(App.UserDetails), "");

            if (string.IsNullOrWhiteSpace(userDetailsStr))
            {
                await Shell.Current.GoToAsync("//LoginPage");
            }
            else
            {
                var encryptedToken = Microsoft.Maui.Storage.Preferences.Get("userToken", string.Empty);
                var tokenDetails = EncryptionHelper.Decrypt(encryptedToken);

                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(tokenDetails) as JwtSecurityToken;

                if (jsonToken.ValidTo < DateTime.Now)
                {
                    await Shell.Current.DisplayAlert("User session expired", "Login again to continue", "OK");
                    await Shell.Current.GoToAsync("//LoginPage");
                }
                else
                {
                    var userInfo = JsonConvert.DeserializeObject<UserBasicInfo>(userDetailsStr);
                    App.UserDetails = userInfo;
                    App.Token = tokenDetails;
                    AppShell.Current.FlyoutHeader = new FlyoutHeaderControl();
                    await Shell.Current.GoToAsync("//DashboardPage");
                }
            }
        }
    }
}
