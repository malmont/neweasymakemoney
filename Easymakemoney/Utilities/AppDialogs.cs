using Easymakemoney.Services.Dialog;

namespace Easymakemoney.Utilities
{
    /// <summary>
    /// Boîtes de dialogue au style de l'appli pour les rares classes qui ne reçoivent pas IDialogService (démarrage, session, fenêtres
    /// du bas). Repli sur la boîte native si le service n'est pas joignable.
    /// </summary>
    public static class AppDialogs
    {
        public static IDialogService? Service => IPlatformApplication.Current?.Services.GetService<IDialogService>();

        public static async Task InfoAsync(string title, string message)
        {
            if (Service is { } dialogs)
                await dialogs.ShowInfoAsync(message, title);
            else if (Shell.Current is { } shell)
                await shell.DisplayAlertAsync(title, message, "OK");
        }

        public static async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        {
            if (Service is { } dialogs)
                return await dialogs.ConfirmAsync(title, message, accept, cancel);
            return Shell.Current is { } shell && await shell.DisplayAlertAsync(title, message, accept, cancel);
        }
    }
}
