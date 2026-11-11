using System.Globalization;
using Easymakemoney.Converters;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>Une variante de l'aperçu, que le gérant coche ou décoche (« le XL noir seulement »).</summary>
    public partial class VariantPromotionChoice : ObservableObject
    {
        public VariantPromotionChoice(VariantPromotionRow row)
        {
            Row = row;
            _isSelected = !row.IsBlocked;
        }

        public VariantPromotionRow Row { get; }

        [ObservableProperty]
        private bool _isSelected;
    }

    /// <summary>
    /// Promotion par variante (lot backend VAR) : remise datée sur les tailles ou couleurs choisies d'un article, variantes à prix propre
    /// comprises. Aperçu de toutes les variantes de l'article ; à l'application, un nouvel aperçu des seules variantes cochées donne
    /// l'empreinte envoyée. Pendant ses dates, elle remplace la promotion de l'article, sans cumul.
    /// </summary>
    public partial class VariantPromotionViewModel : BaseViewModel
    {
        private readonly IVariantPromotionService _service;
        private readonly IDialogService _dialogService;

        public VariantPromotionViewModel(IVariantPromotionService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
            SelectedRounding = Roundings[0];
        }

        public IReadOnlyList<PriceChangeViewModel.Choice> Roundings { get; } = new[]
        {
            new PriceChangeViewModel.Choice("none", "Au centime"),
            new PriceChangeViewModel.Choice("ends_99", "Finir en ,99 (en dessous)"),
            new PriceChangeViewModel.Choice("down_50", "Aux 0,50 inférieurs"),
            new PriceChangeViewModel.Choice("down_100", "À l'unité inférieure"),
        };

        public int ProductId { get; private set; }

        [ObservableProperty]
        private string _scopeText = string.Empty;

        [ObservableProperty]
        private string? _percentText = "20";

        [ObservableProperty]
        private PriceChangeViewModel.Choice? _selectedRounding;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MaxEndDate))]
        private DateTime _startDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _endDate = DateTime.Today.AddDays(14);

        public DateTime MinStartDate => DateTime.Today;
        public DateTime MaxEndDate => StartDate.AddDays(89);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPreview))]
        private VariantPromotionPreview? _preview;

        public bool HasPreview => Preview != null;

        public ObservableCollection<VariantPromotionChoice> Choices { get; } = new();

        [ObservableProperty]
        private bool _allowBelowCost;

        [ObservableProperty]
        private bool _allowBigChange;

        [ObservableProperty]
        private bool _allowReplace;

        [ObservableProperty]
        private bool _allowAboveProductPromotion;

        public bool HasBelowCost => Choices.Any(c => c.Row.Has("BELOW_COST"));
        public bool HasBigChange => Choices.Any(c => c.Row.Has("BIG_CHANGE"));
        public bool HasExisting => Choices.Any(c => c.Row.Has("PROMOTION_EXISTS"));
        public bool HasAboveProductPromotion => Choices.Any(c => c.Row.Has("VARIANT_PRICE_ABOVE_PRODUCT_PROMOTION"));

        public void Init(int productId, string? productName)
        {
            ProductId = productId;
            ScopeText = $"{productName ?? "Cet article"} : cochez les tailles ou couleurs à remiser. Pendant ses dates, la promotion de la variante remplace celle de l'article, sans cumul.";
        }

        partial void OnStartDateChanged(DateTime value)
        {
            if (EndDate < value)
                EndDate = value;
            if (EndDate > MaxEndDate)
                EndDate = MaxEndDate;
            ClearPreview();
        }

        partial void OnEndDateChanged(DateTime value) => ClearPreview();
        partial void OnPercentTextChanged(string? value) => ClearPreview();
        partial void OnSelectedRoundingChanged(PriceChangeViewModel.Choice? value) => ClearPreview();

        private void ClearPreview()
        {
            if (Preview == null)
                return;
            Preview = null;
            Choices.Clear();
            RefreshFlags();
        }

        private void RefreshFlags()
        {
            OnPropertyChanged(nameof(HasBelowCost));
            OnPropertyChanged(nameof(HasBigChange));
            OnPropertyChanged(nameof(HasExisting));
            OnPropertyChanged(nameof(HasAboveProductPromotion));
        }

        /// <summary>Pourcentage de 5 à 90, décimal admis (« 12,5 »).</summary>
        public VariantPromotionRequest? BuildRequest(IReadOnlyList<int>? variantIds, out string? error)
        {
            error = null;
            if (DecimalInputConverter.Parse(PercentText) is not { } percent || percent < 5 || percent > 90)
            {
                error = "La remise doit être entre 5 et 90 %.";
                return null;
            }
            if (EndDate.Date < StartDate.Date)
            {
                error = "La date de fin doit suivre la date de début.";
                return null;
            }

            return new VariantPromotionRequest
            {
                VariantIds = variantIds is { Count: > 0 } ? variantIds.ToList() : null,
                ProductIds = variantIds is { Count: > 0 } ? null : new List<int> { ProductId },
                Percent = Math.Round(percent, 2),
                Rounding = SelectedRounding?.Code ?? "none",
                StartDate = StartDate.Date,
                EndDate = EndDate.Date,
            };
        }

        [RelayCommand]
        private async Task ComputePreview()
        {
            if (IsBusy)
                return;
            var request = BuildRequest(null, out var error);
            if (request == null)
            {
                await _dialogService.ShowInfoAsync(error!);
                return;
            }

            IsBusy = true;
            try
            {
                var preview = await _service.PreviewAsync(request);
                var currency = SiteCurrency.CurrentCurrency;
                Choices.Clear();
                foreach (var row in preview.rows)
                {
                    row.Currency = currency;
                    Choices.Add(new VariantPromotionChoice(row));
                }
                Preview = preview;
                AllowBelowCost = AllowBigChange = AllowReplace = AllowAboveProductPromotion = false;
                RefreshFlags();
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
        private async Task Apply()
        {
            if (Preview == null || IsBusy)
                return;
            if (Preview.IsOff)
            {
                await _dialogService.ShowInfoAsync("Les promotions par variante ne sont pas encore activées sur ce site. Elles le seront quand la caisse, la boutique et l'application seront prêtes.", "Promotion par variante");
                return;
            }

            var selected = Choices.Where(c => c.IsSelected && !c.Row.IsBlocked).Select(c => c.Row.variantId).ToList();
            if (selected.Count == 0)
            {
                await _dialogService.ShowInfoAsync("Cochez au moins une variante à remiser.");
                return;
            }

            var request = BuildRequest(selected, out var error);
            if (request == null)
            {
                await _dialogService.ShowInfoAsync(error!);
                return;
            }

            IsBusy = true;
            try
            {
                // Aperçu des seules variantes cochées : son empreinte accompagne l'application.
                var preview = await _service.PreviewAsync(request);
                var writable = preview.WritableCount(AllowBelowCost, AllowBigChange, AllowReplace, AllowAboveProductPromotion);
                if (writable == 0 || preview.previewHash is not { } hash)
                {
                    IsBusy = false;
                    await _dialogService.ShowInfoAsync("Aucune variante cochée ne peut être remisée avec ces réglages : voyez les alertes de l'aperçu.");
                    return;
                }

                IsBusy = false;
                var period = preview.PeriodText;
                var confirmed = await _dialogService.ConfirmAsync(
                    "Créer la promotion",
                    $"{writable} variante(s) à −{request.Percent.ToString("0.##", CultureInfo.CurrentCulture)} %. {period}.\nElle s'applique en boutique et en caisse ; le prix normal revient tout seul à la fin.",
                    "Créer", "Annuler");
                if (!confirmed)
                    return;

                IsBusy = true;
                var result = await _service.ApplyAsync(request, hash, AllowBelowCost, AllowBigChange, AllowReplace, AllowAboveProductPromotion);
                var skipped = result.skipped.Count;
                VariantPromotionListViewModel.PromotionsChanged = true;
                await _dialogService.ShowInfoAsync(skipped > 0
                    ? $"Promotion créée. {skipped} variante(s) écartée(s) : voyez leurs alertes."
                    : "Promotion créée.", "Promotion par variante");
                if (Shell.Current is { } shell)
                    await shell.GoToAsync("..");
            }
            catch (ApiException ex) when (ex.ApiError.Code is "PRICES_CHANGED" or "NOTHING_TO_APPLY")
            {
                await _dialogService.ShowInfoAsync(ex.ApiError.Code == "PRICES_CHANGED"
                    ? "Des prix ont changé depuis l'aperçu. Vérifiez le nouvel aperçu."
                    : "Rien à appliquer : voyez les alertes de l'aperçu.");
                IsBusy = false;
                await ComputePreview();
            }
            catch (ApiException ex) when (ex.ApiError.Code == "VARIANT_PROMOTIONS_OFF")
            {
                await _dialogService.ShowInfoAsync("Les promotions par variante ne sont pas activées sur ce site.", "Promotion par variante");
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

    /// <summary>Promotions par variante en cours et à venir, avec « Terminer » (tout le lot).</summary>
    public partial class VariantPromotionListViewModel : BaseViewModel
    {
        private readonly IVariantPromotionService _service;
        private readonly IDialogService _dialogService;
        private bool _loaded;

        /// <summary>Mis à vrai quand une promotion est créée : la liste se relit au retour.</summary>
        public static bool PromotionsChanged { get; set; }

        public VariantPromotionListViewModel(IVariantPromotionService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
        }

        public ObservableCollection<VariantPromotionItem> Items { get; } = new();

        [ObservableProperty]
        private bool _isEmpty;

        public async Task InitializeAsync()
        {
            if (_loaded && !PromotionsChanged)
                return;
            _loaded = true;
            PromotionsChanged = false;
            await LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var currency = SiteCurrency.CurrentCurrency;
                var items = await _service.GetPromotionsAsync();
                Items.Clear();
                foreach (var item in items)
                {
                    item.Currency = currency;
                    Items.Add(item);
                }
                IsEmpty = Items.Count == 0;
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
        private async Task End(VariantPromotionItem? item)
        {
            if (item is not { CanEnd: true } || IsBusy)
                return;
            var others = Items.Count(i => i.batchId == item.batchId) - 1;
            var message = (item.status == "upcoming"
                    ? "Elle n'a pas commencé : elle ne s'appliquera jamais."
                    : "Elle s'arrête ce soir ; une vente d'aujourd'hui garde la remise.")
                + (others > 0 ? $" Les {others} autre(s) variante(s) créée(s) en même temps s'arrêtent aussi." : string.Empty);
            if (!await _dialogService.ConfirmAsync("Terminer la promotion", message, "Terminer", "Annuler"))
                return;
            try
            {
                await _service.EndAsync(item.batchId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
            await LoadAsync();
        }
    }
}
