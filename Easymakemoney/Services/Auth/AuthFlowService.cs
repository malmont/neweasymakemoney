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

        // Effacement de la session sans toucher au site
        await _sessionService.ClearSessionAsync();
        _pendingLoginStore.Clear();

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                if (AppShell.Current != null)
                {
                    AppShell.Current.FlyoutHeader = null;
                }
                if (Shell.Current != null) await Shell.Current.GoToAsync("//LoginPage");
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
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            confirm = await Easymakemoney.Utilities.AppDialogs.ConfirmAsync(
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

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                if (AppShell.Current != null)
                {
                    AppShell.Current.FlyoutHeader = null;
                }
                if (Shell.Current != null) await Shell.Current.GoToAsync("//ActivationPage");
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

