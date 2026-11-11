using System.Globalization;
using Easymakemoney.Converters;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Créer une promotion datée : pourcentage, dates, aperçu (rien n'est écrit), puis application après confirmation.
    /// Le prix normal n'est jamais touché et revient tout seul à la fin.
    /// </summary>
    public partial class PromotionCreateViewModel : BaseViewModel
    {
        private readonly IPromotionService _promotionService;
        private readonly IDialogService _dialogService;
        private PromotionRequest? _previewedRequest;

        public PromotionCreateViewModel(IPromotionService promotionService, IDialogService dialogService)
        {
            _promotionService = promotionService;
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

        public int AdviceId { get; private set; }
        public int CommandeId { get; private set; }
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
        private PromotionPreview? _preview;

        public bool HasPreview => Preview != null;

        public ObservableCollection<PromotionPreviewRow> Rows { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ApplyText))]
        [NotifyPropertyChangedFor(nameof(CanApply))]
        private bool _allowBelowCost;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ApplyText))]
        [NotifyPropertyChangedFor(nameof(CanApply))]
        private bool _allowBigChange;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ApplyText))]
        [NotifyPropertyChangedFor(nameof(CanApply))]
        private bool _allowReplace;

        public int WritableCount => Preview?.WritableCount(AllowBelowCost, AllowBigChange, AllowReplace) ?? 0;
        public bool CanApply => WritableCount > 0;
        public string ApplyText => WritableCount == 1 ? "Créer la promotion sur 1 article" : $"Créer la promotion sur {WritableCount} articles";

        public void Init(int adviceId, int commandeId, int productId, double? percent)
        {
            AdviceId = adviceId;
            CommandeId = commandeId;
            ProductId = productId;
            if (percent is { } value)
                PercentText = value.ToString("0.##", CultureInfo.CurrentCulture);

            ScopeText = adviceId > 0
                ? "Articles du conseil du guide. Le conseil sera marqué fait ; son effet est mesuré à partir du début de la promotion."
                : commandeId > 0
                    ? "Articles de cette commande."
                    : "Cet article (ses variantes qui ont leur propre prix ne sont pas concernées).";
            OnPropertyChanged(nameof(CanChooseVariant));
        }

        public bool CanChooseVariant => ProductId > 0 && AdviceId <= 0 && CommandeId <= 0;

        /// <summary>Promotion sur une taille ou une couleur précise de cet article (lot VAR).</summary>
        [RelayCommand]
        private async Task OpenVariantPromotion()
        {
            if (!CanChooseVariant || Shell.Current is not { } shell)
                return;
            await shell.GoToAsync($"{nameof(VariantPromotionPage)}?ProductId={ProductId}");
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
            _previewedRequest = null;
            Rows.Clear();
            RefreshApply();
        }

        private void RefreshApply()
        {
            OnPropertyChanged(nameof(ApplyText));
            OnPropertyChanged(nameof(CanApply));
        }

        private PromotionRequest? BuildRequest(out string? error)
        {
            error = null;
            if (DecimalInputConverter.Parse(PercentText) is not { } percent || percent < 5 || percent > 90)
            {
                error = "La remise doit être entre 5 et 90 %.";
                return null;
            }

            return new PromotionRequest
            {
                AdviceId = AdviceId > 0 ? AdviceId : null,
                CommandeId = AdviceId <= 0 && CommandeId > 0 ? CommandeId : null,
                ProductIds = AdviceId <= 0 && CommandeId <= 0 && ProductId > 0 ? new List<int> { ProductId } : null,
                Percent = percent,
                Rounding = SelectedRounding?.Code ?? "none",
                StartDate = StartDate,
                EndDate = EndDate,
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
                var preview = await _promotionService.PreviewAsync(request);
                Preview = preview;
                _previewedRequest = request;
                AllowBelowCost = AllowBigChange = AllowReplace = false;
                Rows.Clear();
                foreach (var row in preview.rows)
                    Rows.Add(row);
                RefreshApply();
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
            if (Preview?.previewHash is not { } hash || _previewedRequest is not { } request || !CanApply || IsBusy)
                return;

            var confirmed = await _dialogService.ConfirmAsync(
                "Créer la promotion",
                $"{Preview.PeriodText}\nElle s'appliquera en boutique et en caisse. Vous pourrez la terminer à tout moment depuis Gestion › Promotions.",
                "Créer", "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                var result = await _promotionService.ApplyAsync(request, hash, AllowBelowCost, AllowBigChange, AllowReplace);
                await _dialogService.ShowInfoAsync(result.changed == 1 ? "Promotion créée sur 1 article." : $"Promotion créée sur {result.changed} articles.");
                if (Shell.Current is { } shell)
                    await shell.GoToAsync("..");
            }
            catch (ApiException ex) when (ex.ApiError.Code == "PRICES_CHANGED")
            {
                await _dialogService.ShowInfoAsync("Des prix ont changé depuis l'aperçu. Vérifiez le nouvel aperçu.");
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

    /// <summary>Promotions en cours et à venir, avec « Terminer maintenant » pour celles créées depuis l'application.</summary>
    public partial class PromotionListViewModel : BaseViewModel
    {
        private readonly IPromotionService _promotionService;
        private readonly IDialogService _dialogService;

        public PromotionListViewModel(IPromotionService promotionService, IDialogService dialogService)
        {
            _promotionService = promotionService;
            _dialogService = dialogService;
        }

        public ObservableCollection<PromotionItem> Promotions { get; } = new();

        [ObservableProperty]
        private bool _isEmpty;

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                var promotions = await _promotionService.GetPromotionsAsync("all");
                Promotions.Clear();
                foreach (var promotion in promotions.Where(p => p.status is "active" or "planned"))
                    Promotions.Add(promotion);
                IsEmpty = Promotions.Count == 0;
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
        private async Task End(PromotionItem? promotion)
        {
            if (promotion?.batchId is not { } batchId || IsBusy)
                return;

            var confirmed = await _dialogService.ConfirmAsync(
                "Terminer la promotion",
                promotion.status == "planned"
                    ? "Cette promotion n'a pas commencé : elle est annulée pour tous les articles créés avec elle."
                    : "La promotion s'arrête maintenant pour tous les articles créés avec elle. Le prix normal revient aussitôt.",
                "Terminer", "Non");
            if (!confirmed)
                return;

            try
            {
                await _promotionService.EndAsync(batchId);
            }
            catch (ApiException ex) when (ex.ApiError.Code == "PROMOTION_EDITED_SINCE")
            {
                var partial = await _dialogService.ConfirmAsync(
                    "Articles retouchés depuis",
                    "Certains articles ont été modifiés depuis. Terminer la promotion seulement sur les autres ?",
                    "Oui, les autres", "Non");
                if (partial)
                {
                    try
                    {
                        await _promotionService.EndAsync(batchId, onlyUntouched: true);
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

        [RelayCommand]
        private async Task ShowAdminInfo(PromotionItem? promotion)
        {
            if (promotion is { FromAdmin: true })
                await _dialogService.ShowInfoAsync("Cette promotion a été saisie dans l'administration du site : modifiez-la là-bas.");
        }
    }
}
