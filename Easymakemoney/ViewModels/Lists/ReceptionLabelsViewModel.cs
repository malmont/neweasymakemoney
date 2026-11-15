using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Étiquettes d'une commande reçue (backend CB-2) : une ligne par variante reçue avec le nombre proposé (modifiable),
    /// la génération des codes manquants, puis le PDF des planches 50 × 30 mm partagé vers l'imprimante.
    /// </summary>
    public partial class ReceptionLabelsViewModel : BaseViewModel
    {
        private readonly IReceptionLabelsService _service;
        private readonly IDialogService _dialogService;

        public ReceptionLabelsViewModel(IReceptionLabelsService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
        }

        public int CommandeId { get; set; }

        public ObservableCollection<ReceptionLabel> Labels { get; } = new();

        [ObservableProperty]
        private ReceptionLabels? _data;

        [ObservableProperty]
        private bool _showPrice;

        /// <summary>Rouleau thermique (50 × 30 mm, 200 au plus) au lieu de la planche A4 (500 au plus).</summary>
        [ObservableProperty]
        private bool _useRoll;

        private int MaxLabels => UseRoll ? 200 : Data?.maxLabels ?? 500;

        /// <summary>Ouverture du PDF (partage vers l'imprimante) ; les tests la remplacent.</summary>
        public Func<DownloadedFile, Task> OpenFile { get; set; } = OpenWithShareAsync;

        public Task LoadAsync() => RunAsync(async () =>
        {
            if (CommandeId <= 0)
                return;
            Show(await _service.GetAsync(CommandeId));
        });

        [RelayCommand]
        private Task GenerateMissing() => RunAsync(async () =>
        {
            if (Data is not { HasMissing: true } data)
                return;
            var ok = await _dialogService.ConfirmAsync("Codes manquants",
                $"Donner un code-barres aux {data.missing.count} variante(s) qui n'en ont pas ? Aucun code existant n'est remplacé.", "Générer", "Annuler");
            if (!ok)
                return;
            // Les nombres déjà saisis sont gardés : la liste rendue à jour ne doit pas les écraser.
            var typed = Labels.ToDictionary(l => l.variantId, l => l.Copies);
            var result = await _service.GenerateMissingAsync(CommandeId);
            foreach (var label in result.labels)
                if (typed.TryGetValue(label.variantId, out var copies))
                    label.Copies = copies;
            Show(result);
            var count = result.generated?.Count ?? 0;
            await _dialogService.ShowInfoAsync(count == 1 ? "1 code-barres généré." : $"{count} codes-barres générés.");
        });

        [RelayCommand]
        private Task Print() => RunAsync(async () =>
        {
            if (Data == null)
                return;
            var lines = Labels.Where(l => l.printable && l.Copies > 0).Select(l => (l.variantId, l.Copies)).ToList();
            if (lines.Count == 0)
                throw new UserMessageException(Data.HasMissing
                    ? "Aucune étiquette à imprimer : générez d'abord les codes manquants, ou indiquez un nombre d'étiquettes."
                    : "Indiquez au moins une étiquette à imprimer.");
            var total = lines.Sum(l => l.Item2);
            if (total > MaxLabels)
                throw new UserMessageException($"{total} étiquettes demandées : {MaxLabels} au plus par impression{(UseRoll ? " en rouleau" : string.Empty)}.");
            var file = await _service.PdfAsync(CommandeId, lines, ShowPrice, UseRoll ? "roll-50x30" : "a4-50x30");
            await OpenFile(file);
        });

        private void Show(ReceptionLabels data)
        {
            Data = data;
            Labels.Clear();
            foreach (var label in data.labels)
                Labels.Add(label);
        }

        private async Task RunAsync(Func<Task> action)
        {
            if (IsBusy)
                return;
            IsBusy = true;
            try
            {
                await action();
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

        private static async Task OpenWithShareAsync(DownloadedFile file)
        {
            var path = Path.Combine(FileSystem.CacheDirectory, $"etiquettes-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            await File.WriteAllBytesAsync(path, file.Content);
            await Share.Default.RequestAsync(new ShareFileRequest { Title = "Étiquettes", File = new ShareFile(path, "application/pdf") });
        }
    }
}
