using Easymakemoney.Services.Dialog;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Marchandise à recevoir : les lignes d'achat pas encore reçues, regroupées par commande.
    /// « Tout recevoir » met le stock à jour (seule écriture de stock de la gestion, côté backend).
    /// </summary>
    public partial class ToReceiveViewModel : BaseViewModel
    {
        private readonly IToReceiveService _toReceiveService;
        private readonly ICommandeReceptionService _reception;
        private readonly IDialogService _dialogService;

        /// <summary>Choix dans une liste (variante reçue) ; « Plus tard » ou rien = reportée. Remplaçable dans les tests.</summary>
        public Func<string, string[], Task<string?>> ChooseAsync { get; set; } = async (title, labels) =>
            Shell.Current is { } shell ? await shell.DisplayActionSheetAsync(title, "Plus tard", null, labels) : null;

        public ToReceiveViewModel(IToReceiveService toReceiveService, ICommandeReceptionService reception, IDialogService dialogService)
        {
            _toReceiveService = toReceiveService;
            _reception = reception;
            _dialogService = dialogService;
        }

        public ObservableCollection<ToReceiveGroup> Groups { get; } = new();

        [ObservableProperty]
        private bool _isEmpty;

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                var lines = await _toReceiveService.GetLinesAsync();
                Groups.Clear();
                foreach (var group in ToReceiveGroup.Build(lines))
                    Groups.Add(group);
                IsEmpty = Groups.Count == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ReceiveAll(ToReceiveGroup? group)
        {
            if (group == null || IsBusy)
                return;

            var confirmed = await _dialogService.ConfirmAsync(
                "Marchandise reçue",
                $"Ajouter au stock tout ce qui reste à recevoir de « {group.Name} » ({group.RemainingText}) ?",
                "Recevoir", "Annuler");
            if (!confirmed)
                return;

            // 1. Tout ce qui peut être reçu l'est ; les articles à plusieurs variantes reviennent dans needsVariant.
            var first = await TryReceiveAsync(group.CommandeId, null);
            if (first == null)
                return;

            // 2. Le gérant dit quelle variante il a reçue, puis la même route reçoit ces lignes.
            var choices = new List<(int LineId, int VariantId)>();
            var postponed = 0;
            foreach (var need in first.NeedsVariant)
            {
                if (need.Variants.Count == 0)
                {
                    postponed++;
                    continue;
                }

                var labels = need.ChoiceLabels();
                var choice = await ChooseAsync($"Quelle variante de « {need.ProductName ?? "cet article"} » avez-vous reçue ?", labels);
                var index = Array.IndexOf(labels, choice);
                if (index < 0)
                    postponed++;
                else
                    choices.Add((need.LineId, need.Variants[index].Id));
            }

            var received = first.ReceivedQuantity;
            var last = first;
            if (choices.Count > 0)
            {
                var second = await TryReceiveAsync(group.CommandeId, CommandeReception.VariantChoicesBody(choices));
                if (second != null)
                {
                    received += second.ReceivedQuantity;
                    last = second;
                }
                else
                {
                    postponed += choices.Count;
                }
            }

            await _dialogService.ShowInfoAsync(CommandeReception.Summary(received, last, postponed));
            await LoadAsync();
        }

        private async Task<CommandeReception?> TryReceiveAsync(int commandeId, JObject? body)
        {
            try
            {
                return await _reception.ReceiveAsync(commandeId, body);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
                return null;
            }
        }

        [RelayCommand]
        private async Task OpenCommande(ToReceiveGroup? group)
        {
            if (group != null && Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(ListNewProductPage)}?CommandId={group.CommandeId}");
        }
    }
}
