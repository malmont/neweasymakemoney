using System.Diagnostics;
using Easymakemoney.Components;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Auth;
using Easymakemoney.Services.Tenant;

namespace Easymakemoney.Services.Dialog;

/// <summary>
/// Messages, erreurs, confirmations et définitions, dans la boîte de dialogue au style de l'application (UiDialogView).
/// Repli sur la boîte native quand la surimpression n'est pas possible.
/// </summary>
public class DialogService : IDialogService
{
    private readonly SemaphoreSlim _errorDialogLock = new(1, 1);

    /// <summary>Une boîte à la fois : la suivante attend que la précédente soit fermée.</summary>
    private readonly SemaphoreSlim _queue = new(1, 1);

    public async Task ShowErrorAsync(Exception exception)
    {
        // Règle P3 : Fin de session et site absent restent silencieux (journal seulement)
        if (exception is SessionExpiredException or AuthenticationMissingException or TenantMissingException)
        {
            Debug.WriteLine($"[DialogService] Exception silencieuse : {exception.GetType().Name} - {exception.Message}");
            return;
        }

        // Lot MOD : module de gestion non accordé : l'écran « non activée » s'ouvre à la place (ModuleGateHandler)
        if (exception is ApiException { ApiError.Code: Models.ModuleInactiveInfo.Code })
        {
            Debug.WriteLine($"[DialogService] Module inactif, écran dédié : {exception.Message}");
            return;
        }

        // Règle P2 : Un seul message d'erreur affiché à la fois
        if (!_errorDialogLock.Wait(0))
        {
            Debug.WriteLine($"[DialogService] Erreur supplémentaire ignorée (dialogue déjà ouvert) : {exception.GetType().Name} - {exception.Message}");
            return;
        }

        try
        {
            string message;
            if (exception is ApiException apiEx)
            {
                message = apiEx.ApiError.Message;
                if (apiEx.ApiError.Errors != null && apiEx.ApiError.Errors.Count > 0)
                {
                    var details = string.Join("\n", apiEx.ApiError.Errors.Select(e =>
                        string.IsNullOrWhiteSpace(e.Path) ? $"• {e.Message}" : $"• {e.Path}: {e.Message}"));
                    message = $"{message}\n{details}";
                }
            }
            else
            {
                Debug.WriteLine($"[DialogService] Exception imprévue : {exception.GetType().Name} - {exception.Message}");
                message = "Une erreur est survenue, réessayez.";
            }

            await PresentAsync(UiDialogKind.Error, "Erreur", message, "OK", null);
        }
        finally
        {
            _errorDialogLock.Release();
        }
    }

    public Task ShowInfoAsync(string message, string title = "Information") =>
        PresentAsync(UiDialogKind.Info, title, message, "OK", null);

    public Task<bool> ConfirmAsync(string title, string message, string accept = "Oui", string cancel = "Non") =>
        PresentAsync(UiDialogKind.Confirm, title, message, accept, cancel);

    private async Task<bool> PresentAsync(UiDialogKind kind, string title, string message, string accept, string? cancel)
    {
        await _queue.WaitAsync();
        try
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Règle P2 : Protection en cas d'absence de Shell.Current
                var shell = Shell.Current;
                if (shell == null)
                {
                    Debug.WriteLine($"[DialogService] Shell.Current est nul, impossible d'afficher : {title} - {message}");
                    return false;
                }

                var view = new UiDialogView(kind, title, message, accept, cancel);
                Action? detach = null;
                try
                {
                    detach = DialogOverlay.Attach(view);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DialogService] Surimpression impossible, boîte native : {ex.Message}");
                }

                if (detach == null)
                {
                    if (cancel == null)
                    {
                        await shell.DisplayAlertAsync(title, message, accept);
                        return true;
                    }
                    return await shell.DisplayAlertAsync(title, message, accept, cancel);
                }

                try
                {
                    await view.AppearAsync();
                    return await view.Result;
                }
                finally
                {
                    detach();
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DialogService] Échec affichage dialogue : {ex.Message}");
            return false;
        }
        finally
        {
            _queue.Release();
        }
    }
}
