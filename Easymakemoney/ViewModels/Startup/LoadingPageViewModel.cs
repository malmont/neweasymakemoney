using Easymakemoney.Controls;
using Easymakemoney.Models.Tenant;
using Easymakemoney.Services;
using Easymakemoney.Services.Auth;
using Easymakemoney.Services.Session;
using Easymakemoney.Services.Site;
using Easymakemoney.Services.Tenant;

namespace Easymakemoney.ViewModels.Startup
{
    public class LoadingPageViewModel
    {
        private readonly ITenantStore _tenantStore;
        private readonly ISiteService _siteService;
        private readonly ISiteDataCleaner _siteDataCleaner;
        private readonly ISessionService _sessionService;
        private readonly IJwtService _jwtService;
        private readonly ITokenRefresher _tokenRefresher;

        public LoadingPageViewModel(
            ITenantStore tenantStore,
            ISiteService siteService,
            ISiteDataCleaner siteDataCleaner,
            ISessionService sessionService,
            IJwtService jwtService,
            ITokenRefresher tokenRefresher,
            bool autoStart = true)
        {
            _tenantStore = tenantStore;
            _siteService = siteService;
            _siteDataCleaner = siteDataCleaner;
            _sessionService = sessionService;
            _jwtService = jwtService;
            _tokenRefresher = tokenRefresher;

            // Les tests passent autoStart: false, règlent les délégués ci-dessous, puis attendent RunStartupAsync().
            StartupTask = autoStart ? RunStartupAsync() : Task.CompletedTask;
        }

        /// <summary>Démarrage lancé par le constructeur (null-safe : jamais d'exception non attrapée).</summary>
        public Task StartupTask { get; }

        /// <summary>Navigation (point d'entrée des tests).</summary>
        public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;

        /// <summary>Message d'information (point d'entrée des tests).</summary>
        public Func<string, string, Task> ShowInfoAsync { get; set; } = (title, message) => Easymakemoney.Utilities.AppDialogs.InfoAsync(title, message);

        /// <summary>Passage sur le fil principal (point d'entrée des tests : exécution directe).</summary>
        public Func<Func<Task>, Task> RunOnMainThreadAsync { get; set; } = action => MainThread.InvokeOnMainThreadAsync(action);

        /// <summary>Lancement de la revérification du site en arrière-plan (point d'entrée des tests : rien, ou exécution attendue).</summary>
        public Action<Func<Task>> StartInBackground { get; set; } = work => _ = Task.Run(work);

        public async Task RunStartupAsync()
        {
            try
            {
                // Règle 6.1 : effacement au premier lancement
                await _tenantStore.EnsureFirstRunCleanAsync();

                // Règle 6.2 : lecture du site enregistré
                var tenant = await _tenantStore.GetTenantAsync();
                if (tenant == null)
                {
                    await NavigateAsync("//ActivationPage");
                    return;
                }

                // Règle 6.3 & L9 : revérification en arrière-plan sans retarder l'affichage
                StartInBackground(() => RecheckTenantInBackgroundAsync(tenant));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadingPageViewModel] Erreur initialisation startup : {ex.Message}");
                await NavigateAsync("//ActivationPage");
                return;
            }

            // Vérification de session sécurisée (Lot 2)
            await CheckSessionAndNavigateAsync();
        }

        public async Task RecheckTenantInBackgroundAsync(TenantContext tenant)
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

                    await RunOnMainThreadAsync(async () =>
                    {
                        await ShowInfoAsync("Information", "Ce site n'est plus disponible.");
                        await NavigateAsync("//ActivationPage");
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

        private async Task CheckSessionAndNavigateAsync()
        {
            try
            {
                var token = await _sessionService.GetTokenAsync();
                var refreshToken = await _sessionService.GetRefreshTokenAsync();

                // Cas 1 : pas de session (aucun jeton ni refresh token)
                if (string.IsNullOrWhiteSpace(token) && string.IsNullOrWhiteSpace(refreshToken))
                {
                    await NavigateAsync("//LoginPage");
                    return;
                }

                // Cas 2 : jeton présent et encore valide (comparaison UTC avec 60s de marge)
                if (!string.IsNullOrWhiteSpace(token))
                {
                    var expUtc = _jwtService.GetExpirationUtc(token);
                    if (expUtc.HasValue && expUtc.Value > DateTime.UtcNow.AddSeconds(60))
                    {
                        var user = await _sessionService.GetUserAsync();
                        if (AppShell.Current != null)
                        {
                            AppShell.Current.FlyoutHeader = new FlyoutHeaderControl(user);
                        }
                        await NavigateAsync("//DashboardPage");
                        return;
                    }
                }

                // Cas 3 : jeton expiré ou absent, mais refresh token présent
                if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    var refreshResult = await _tokenRefresher.RefreshAsync(token);

                    if (refreshResult.IsSuccess)
                    {
                        var user = await _sessionService.GetUserAsync();
                        if (AppShell.Current != null)
                        {
                            AppShell.Current.FlyoutHeader = new FlyoutHeaderControl(user);
                        }
                        await NavigateAsync("//DashboardPage");
                        return;
                    }

                    if (refreshResult.Status == RefreshStatus.Rejected)
                    {
                        // Refus serveur (400 ou 401) : TokenRefresher a purgé la session
                        await NavigateAsync("//LoginPage");
                        return;
                    }

                    // Serveur injoignable (NetworkError) : la session est conservée
                    var cachedUser = await _sessionService.GetUserAsync();
                    if (AppShell.Current != null)
                    {
                        AppShell.Current.FlyoutHeader = new FlyoutHeaderControl(cachedUser);
                    }
                    await NavigateAsync("//DashboardPage");
                    return;
                }

                await NavigateAsync("//LoginPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadingPageViewModel] Erreur contrôle session : {ex.Message}");
                await NavigateAsync("//LoginPage");
            }
        }

    }
}
