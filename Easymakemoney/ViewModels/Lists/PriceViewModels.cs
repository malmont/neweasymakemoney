using System.Globalization;
using Easymakemoney.Converters;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Changer des prix de vente : aperçu (rien n'est écrit), puis application après confirmation.
    /// Ouvert depuis un conseil du guide (hausse sur ce qui part vite) ou une commande fournisseur (coefficient sur le coût).
    /// </summary>
    public partial class PriceChangeViewModel : BaseViewModel
    {
        public sealed record Choice(string Code, string Label)
        {
            public override string ToString() => Label;
        }

        private readonly IPriceService _priceService;
        private readonly IDialogService _dialogService;
        private PriceChangeRequest? _previewedRequest;

        public PriceChangeViewModel(IPriceService priceService, IDialogService dialogService)
        {
            _priceService = priceService;
            _dialogService = dialogService;
            SelectedMode = Modes[0];
            SelectedRounding = Roundings[0];
        }

        public IReadOnlyList<Choice> Modes { get; } = new[]
        {
            new Choice("percent", "Pourcentage du prix actuel"),
            new Choice("coefficient", "Coefficient sur le coût d'achat"),
        };

        public IReadOnlyList<Choice> Roundings { get; } = new[]
        {
            new Choice("none", "Au centime"),
            new Choice("ends_99", "Finir en ,99"),
            new Choice("up_50", "Aux 0,50 supérieurs"),
            new Choice("up_100", "À l'unité supérieure"),
        };

        public int AdviceId { get; private set; }
        public int CommandeId { get; private set; }
        public int ProductId { get; private set; }

        [ObservableProperty]
        private string _scopeText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ValueLabel))]
        [NotifyPropertyChangedFor(nameof(ValuePlaceholder))]
        private Choice? _selectedMode;

        [ObservableProperty]
        private Choice? _selectedRounding;

        [ObservableProperty]
        private string? _valueText;

        public string ValueLabel => SelectedMode?.Code == "coefficient" ? "Coefficient (prix = coût × coefficient)" : "Variation (en %, négatif pour baisser)";
        public string ValuePlaceholder => SelectedMode?.Code == "coefficient" ? "Ex. : 2,2" : "Ex. : 5";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPreview))]
        private PricePreview? _preview;

        public bool HasPreview => Preview != null;

        public ObservableCollection<PricePreviewRow> Rows { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ApplyText))]
        [NotifyPropertyChangedFor(nameof(CanApply))]
        private bool _allowBelowCost;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ApplyText))]
        [NotifyPropertyChangedFor(nameof(CanApply))]
        private bool _allowBigChange;

        public int WritableCount => Preview?.WritableCount(AllowBelowCost, AllowBigChange) ?? 0;
        public bool CanApply => WritableCount > 0;
        public string ApplyText => WritableCount == 1 ? "Appliquer 1 prix" : $"Appliquer {WritableCount} prix";

        /// <summary>Paramètres : AdviceId et Percent (conseil), CommandeId (commande fournisseur) ou ProductId (un article).</summary>
        public void Init(int adviceId, int commandeId, double? percent, int productId = 0)
        {
            AdviceId = adviceId;
            CommandeId = commandeId;
            ProductId = productId;
            if (productId > 0 && adviceId <= 0 && commandeId <= 0)
            {
                ScopeText = "Cet article et ses variantes qui ont leur propre prix.";
                SelectedMode = Modes[0];
                ValueText = "5";
                return;
            }
            if (adviceId > 0)
            {
                ScopeText = "Articles du conseil du guide. Le conseil sera marqué fait et son effet mesuré sur 14 jours.";
                SelectedMode = Modes[0];
                ValueText = percent is { } value ? value.ToString("0.##", CultureInfo.CurrentCulture) : "5";
            }
            else if (commandeId > 0)
            {
                ScopeText = "Articles de cette commande. En coefficient, le coût est celui payé sur la commande.";
                SelectedMode = Modes[1];
                ValueText = "2";
            }
        }

        private PriceChangeRequest? BuildRequest(out string? error)
        {
            error = null;
            if (DecimalInputConverter.Parse(ValueText) is not { } value)
            {
                error = SelectedMode?.Code == "coefficient" ? "Saisissez un coefficient (par exemple 2,2)." : "Saisissez un pourcentage (par exemple 5 ou -10).";
                return null;
            }

            return new PriceChangeRequest
            {
                AdviceId = AdviceId > 0 ? AdviceId : null,
                CommandeId = AdviceId > 0 ? null : CommandeId > 0 ? CommandeId : null,
                ProductIds = AdviceId <= 0 && CommandeId <= 0 && ProductId > 0 ? new List<int> { ProductId } : null,
                Mode = SelectedMode?.Code ?? "percent",
                Value = value,
                Rounding = SelectedRounding?.Code ?? "none",
            };
        }

        [RelayCommand]
        private async Task ComputePreview()
        {
            if (IsBusy)
                return;

            var request = BuildRequest(out var error);
            if (request == null)
            {
                await _dialogService.ShowInfoAsync(error!);
                return;
            }

            IsBusy = true;
            try
            {
                ShowPreview(await _priceService.PreviewAsync(request), request);
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

        private void ShowPreview(PricePreview preview, PriceChangeRequest request)
        {
            Preview = preview;
            _previewedRequest = request;
            AllowBelowCost = false;
            AllowBigChange = false;
            Rows.Clear();
            foreach (var row in preview.rows)
                Rows.Add(row);
            OnPropertyChanged(nameof(ApplyText));
            OnPropertyChanged(nameof(CanApply));
        }

        partial void OnValueTextChanged(string? value) => ClearPreview();
        partial void OnSelectedModeChanged(Choice? value) => ClearPreview();
        partial void OnSelectedRoundingChanged(Choice? value) => ClearPreview();

        /// <summary>Un réglage changé rend l'aperçu périmé : il faut le recalculer avant d'appliquer.</summary>
        private void ClearPreview()
        {
            if (Preview == null)
                return;
            Preview = null;
            _previewedRequest = null;
            Rows.Clear();
            OnPropertyChanged(nameof(ApplyText));
            OnPropertyChanged(nameof(CanApply));
        }

        [RelayCommand]
        private async Task Apply()
        {
            if (Preview?.previewHash is not { } hash || _previewedRequest is not { } request || !CanApply || IsBusy)
                return;

            var confirmed = await _dialogService.ConfirmAsync(
                "Changer les prix de vente",
                $"{ApplyText.Replace("Appliquer", "Changer")} en boutique et en caisse. Vous pourrez annuler pendant 24 h depuis Gestion › Changements de prix.",
                "Changer les prix", "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                var result = await _priceService.ApplyAsync(request, hash, AllowBelowCost, AllowBigChange);
                await _dialogService.ShowInfoAsync(result.changed == 1
                    ? "1 prix changé. Annulation possible pendant 24 h."
                    : $"{result.changed} prix changés. Annulation possible pendant 24 h.");
                if (Shell.Current is { } shell)
                    await shell.GoToAsync("..");
            }
            catch (ApiException ex) when (ex.ApiError.Code == "PRICES_CHANGED")
            {
                // Un prix a bougé entre l'aperçu et l'application : on recalcule l'aperçu et on laisse le gérant revoir.
                await _dialogService.ShowInfoAsync("Des prix ont changé depuis l'aperçu. Vérifiez le nouvel aperçu avant d'appliquer.");
                IsBusy = false;
                await ComputePreview();
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
    }

    /// <summary>Journal des changements de prix, avec annulation pendant 24 h.</summary>
    public partial class PriceBatchListViewModel : BaseViewModel
    {
        private readonly IPriceService _priceService;
        private readonly IDialogService _dialogService;

        public PriceBatchListViewModel(IPriceService priceService, IDialogService dialogService)
        {
            _priceService = priceService;
            _dialogService = dialogService;
        }

        public ObservableCollection<PriceBatch> Batches { get; } = new();

        [ObservableProperty]
        private bool _isEmpty;

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                var batches = await _priceService.GetBatchesAsync();
                Batches.Clear();
                foreach (var batch in batches)
                    Batches.Add(batch);
                IsEmpty = Batches.Count == 0;
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
        private async Task Undo(PriceBatch? batch)
        {
            if (batch == null || !batch.CanUndo || IsBusy)
                return;

            var confirmed = await _dialogService.ConfirmAsync(
                "Annuler ce changement",
                $"Remettre les anciens prix ({batch.changed} prix) en boutique et en caisse ?",
                "Remettre les anciens prix", "Non");
            if (!confirmed)
                return;

            try
            {
                var result = await _priceService.UndoAsync(batch.id);
                await _dialogService.ShowInfoAsync(UndoMessage(result));
            }
            catch (ApiException ex) when (ex.ApiError.Code == "PRICE_EDITED_SINCE")
            {
                var partial = await _dialogService.ConfirmAsync(
                    "Prix retouchés depuis",
                    "Certains prix ont été modifiés depuis ce changement. Remettre seulement les autres à leur ancien prix ?",
                    "Oui, les autres", "Non");
                if (partial)
                {
                    try
                    {
                        await _dialogService.ShowInfoAsync(UndoMessage(await _priceService.UndoAsync(batch.id, onlyUntouched: true)));
                    }
                    catch (Exception inner)
                    {
                        Debug.WriteLine(inner);
                        await _dialogService.ShowErrorAsync(inner);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }

            await LoadAsync();
        }

        private static string UndoMessage(PriceUndoResult result)
        {
            var text = result.restored == 1 ? "1 prix remis à l'ancien." : $"{result.restored} prix remis à l'ancien.";
            if (result.adviceRestored)
                text += " Le conseil du guide redevient à faire.";
            return text;
        }
    }
}
