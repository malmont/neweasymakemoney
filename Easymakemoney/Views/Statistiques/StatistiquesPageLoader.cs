using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.Views.Statistiques
{
    /// <summary>
    /// Chargement d'un écran de statistiques : une seule fois par ouverture, erreurs affichées au lieu de faire planter l'application.
    /// Le choix Semaine / Mois / Année ferme une fenêtre modale, ce qui rappelle OnAppearing : recharger à ce moment-là
    /// remettait l'écran sur « Semaine » et écrasait le choix du gérant.
    /// </summary>
    public sealed class StatistiquesPageLoader
    {
        private bool _loaded;

        /// <param name="load">Chargement et premier affichage.</param>
        /// <param name="onNotFound">Facultatif : traite un 404 comme « rien à afficher » (caisse sans transaction).</param>
        public async Task RunOnceAsync(Func<Task> load, Func<ApiException, Task<bool>>? onNotFound = null)
        {
            if (_loaded)
                return;
            _loaded = true;

            try
            {
                await load();
            }
            catch (Exception ex)
            {
                // Cas attendu (caisse sans transaction…) : déjà traité par l'écran, liste vide et message.
                if (ex is ApiException api && onNotFound != null && await onNotFound(api))
                    return;

                _loaded = false; // nouvel essai au prochain affichage
                Debug.WriteLine(ex);
                var dialog = IPlatformApplication.Current?.Services.GetService<IDialogService>();
                if (dialog != null)
                    await dialog.ShowErrorAsync(ex);
            }
        }
    }
}
