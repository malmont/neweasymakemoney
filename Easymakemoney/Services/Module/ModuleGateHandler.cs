using System.Net;

namespace Easymakemoney.Services.Module;

/// <summary>
/// Lot MOD : repère le 403 MODULE_INACTIVE du module « management » et prévient le notificateur, qui ouvre
/// l'écran « Application de gestion non activée ». La réponse remonte telle quelle : l'appel échoue comme avant
/// (ApiException), mais sans boîte d'erreur (DialogService la tait).
/// </summary>
public class ModuleGateHandler : DelegatingHandler
{
    private readonly IModuleInactiveNotifier _notifier;

    public ModuleGateHandler(IModuleInactiveNotifier notifier)
    {
        _notifier = notifier;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Forbidden)
            return response;

        try
        {
            // Corps mis en mémoire : HttpService le relira pour construire l'erreur.
            await response.Content.LoadIntoBufferAsync();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (ModuleInactiveInfo.TryParse(json) is { IsManagement: true } info)
                _ = _notifier.NotifyAsync(info);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ModuleGateHandler] Lecture du 403 impossible : {ex.Message}");
        }

        return response;
    }
}
