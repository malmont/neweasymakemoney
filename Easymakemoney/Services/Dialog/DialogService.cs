using System.Diagnostics;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Auth;
using Easymakemoney.Services.Tenant;

namespace Easymakemoney.Services.Dialog;

public class DialogService : IDialogService
{
    private readonly SemaphoreSlim _errorDialogLock = new(1, 1);

    public async Task ShowErrorAsync(Exception exception)
    {
        // Règle P3 : Fin de session et site absent restent silencieux (journal seulement)
        if (exception is SessionExpiredException or AuthenticationMissingException or TenantMissingException)
        {
            Debug.WriteLine($"[DialogService] Exception silencieuse : {exception.GetType().Name} - {exception.Message}");
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
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Règle P2 : Protection en cas d'absence de Shell.Current
                var shell = Shell.Current;
                if (shell == null)
                {
                    Debug.WriteLine($"[DialogService] Shell.Current est nul, impossible d'afficher le dialogue : {exception.Message}");
                    return;
                }

                string title = "Erreur";
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

                await shell.DisplayAlertAsync(title, message, "OK");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DialogService] Échec affichage dialogue : {ex.Message}");
        }
        finally
        {
            _errorDialogLock.Release();
        }
    }

    public async Task ShowInfoAsync(string message, string title = "Information")
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var shell = Shell.Current;
                if (shell != null)
                {
                    await shell.DisplayAlertAsync(title, message, "OK");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DialogService] Échec ShowInfoAsync : {ex.Message}");
        }
    }

    public async Task<bool> ConfirmAsync(string title, string message, string accept = "Oui", string cancel = "Non")
    {
        try
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var shell = Shell.Current;
                if (shell != null)
                {
                    return await shell.DisplayAlertAsync(title, message, accept, cancel);
                }
                return false;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DialogService] Échec ConfirmAsync : {ex.Message}");
            return false;
        }
    }
}

