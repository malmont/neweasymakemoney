using System.Diagnostics;
using System.Text;
using Easymakemoney.Models.Login;
using Easymakemoney.Services.Session;
using Easymakemoney.Services.Tenant;
using Newtonsoft.Json;

namespace Easymakemoney.Services.Auth;

public class AuthFlowService : IAuthFlowService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISessionService _sessionService;
    private readonly ISiteDataCleaner _siteDataCleaner;
    private readonly IPendingLoginStore _pendingLoginStore;

    public AuthFlowService(
        IHttpClientFactory httpClientFactory,
        ISessionService sessionService,
        ISiteDataCleaner siteDataCleaner,
        IPendingLoginStore pendingLoginStore)
    {
        _httpClientFactory = httpClientFactory;
        _sessionService = sessionService;
        _siteDataCleaner = siteDataCleaner;
        _pendingLoginStore = pendingLoginStore;
    }

    /// <summary>Passage sur le fil principal (point d'entrée des tests : exécution directe).</summary>
    public Func<Func<Task>, Task> RunOnMainThreadAsync { get; set; } = action => MainThread.InvokeOnMainThreadAsync(action);

    /// <summary>Confirmation : titre, message, accepter, annuler (point d'entrée des tests).</summary>
    public Func<string, string, string, string, Task<bool>> ConfirmAsync { get; set; } =
        (title, message, accept, cancel) => Easymakemoney.Utilities.AppDialogs.ConfirmAsync(title, message, accept, cancel);

    /// <summary>Navigation (point d'entrée des tests).</summary>
    public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;

    public async Task LogoutAsync()
    {
        try
        {
            var refreshToken = await _sessionService.GetRefreshTokenAsync();
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await RevokeTokenOnServerAsync(refreshToken);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AuthFlowService] Erreur lors de l'appel logout : {ex.Message}");
        }

        // Effacement de la session sans toucher au site ; le compte suivant ne doit pas hériter du bandeau « hors connexion ».
        await _sessionService.ClearSessionAsync();
        Easymakemoney.Services.Offline.OfflineState.Online();
        _pendingLoginStore.Clear();

        await RunOnMainThreadAsync(async () =>
        {
            try
            {
                if (AppShell.Current != null)
                {
                    AppShell.Current.FlyoutHeader = null;
                }
                await NavigateAsync("//LoginPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AuthFlowService] Erreur navigation logout : {ex.Message}");
            }
        });
    }

    public async Task<bool> PromptAndChangeSiteAsync()
    {
        bool confirm = false;
        await RunOnMainThreadAsync(async () =>
        {
            confirm = await ConfirmAsync(
                "Changer de site",
                "Changer de site ? Vous serez déconnecté et les données de ce site seront effacées de l'appareil.",
                "Changer de site",
                "Annuler");
        });

        if (!confirm)
        {
            return false;
        }

        try
        {
            var refreshToken = await _sessionService.GetRefreshTokenAsync();
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await RevokeTokenOnServerAsync(refreshToken);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AuthFlowService] Erreur lors de la révocation du jeton : {ex.Message}");
        }

        // Effacement complet de toutes les données du site et de la session
        await _siteDataCleaner.ClearAllAsync();
        _pendingLoginStore.Clear();

        await RunOnMainThreadAsync(async () =>
        {
            try
            {
                if (AppShell.Current != null)
                {
                    AppShell.Current.FlyoutHeader = null;
                }
                await NavigateAsync("//ActivationPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AuthFlowService] Erreur navigation vers activation : {ex.Message}");
            }
        });

        return true;
    }

    private async Task RevokeTokenOnServerAsync(string refreshToken)
    {
        try
        {
            var payload = new LogoutRequest { refresh_token = refreshToken };
            var json = JsonConvert.SerializeObject(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, "logout");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient("auth");
            using var response = await client.SendAsync(request);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AuthFlowService] Erreur serveur révocation (ignorée) : {ex.Message}");
        }
    }
}

