using System.Globalization;
using Easymakemoney.Converters;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Statistiques
{
    /// <summary>
    /// Mettre une location en promotion (lot backend LOC3) : remise, dates, jours de la semaine, aperçu (rien n'est écrit), puis application
    /// tout ou rien. Depuis un article de Statistiques › Locations ou depuis le conseil « jours creux en semaine » (RENT2) du guide.
    /// </summary>
    public partial class RentalPromotionViewModel : BaseViewModel
    {
        private readonly IRentalPromotionService _service;
        private readonly IDialogService _dialogService;
        private RentalPromotionRequest? _previewedRequest;

        public RentalPromotionViewModel(IRentalPromotionService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
            Weekdays = Enumerable.Range(1, 7)
                .Select(d => new StatsChoice(d.ToString(CultureInfo.InvariantCulture), char.ToUpperInvariant(RentalWeekdays.Name(d)[0]) + RentalWeekdays.Name(d)[1..3] + "."))
                .ToList();
            foreach (var day in Weekdays)
                day.IsSelected = true;
        }

        public int AdviceId { get; private set; }
        public int ProductId { get; private set; }

        public IReadOnlyList<StatsChoice> Weekdays { get; }

        [ObservableProperty]
        private string _scopeText = string.Empty;

        [ObservableProperty]
        private string? _percentText = "20";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MaxEndDate))]
        private DateTime _startDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _endDate = DateTime.Today.AddDays(29);

        public DateTime MinStartDate => DateTime.Today;
        public DateTime MaxEndDate => StartDate.AddDays(89);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPreview), nameof(CanApply))]
        private RentalPromotionPreview? _preview;

        public bool HasPreview => Preview != null;
        public bool CanApply => Preview?.CanApply == true;

        public ObservableCollection<RentalPromotionPreviewRow> Rows { get; } = new();

        /// <param name="weekdays">Jours du conseil, séparés par des virgules (« 1,2,3,4 »).</param>
        public void Init(int adviceId, int productId, string? productName, double? percent, string? weekdays)
        {
            AdviceId = adviceId;
            ProductId = productId;
            if (percent is { } value)
                PercentText = Math.Round(value).ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(weekdays))
            {
                var days = weekdays.Split(',').Select(part => int.TryParse(part, out var d) ? d : 0).Where(d => d is >= 1 and <= 7).ToHashSet();
                if (days.Count > 0)
                    foreach (var day in Weekdays)
                        day.IsSelected = days.Contains(int.Parse(day.Code, CultureInfo.InvariantCulture));
            }
            ScopeText = adviceId > 0
                ? "Articles du conseil du guide. Le conseil sera marqué fait ; son effet est mesuré à partir du début de la promotion."
                : $"{productName ?? "Cet article"} : la remise porte sur le tarif de location, jamais sur la caution ni le supplément passagers.";
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

        [RelayCommand]
        private void ToggleWeekday(string code)
        {
            var day = Weekdays.FirstOrDefault(d => d.Code == code);
            if (day == null)
                return;
            // Au moins un jour reste coché.
            if (day.IsSelected && Weekdays.Count(d => d.IsSelected) == 1)
                return;
            day.IsSelected = !day.IsSelected;
            ClearPreview();
        }

        private void ClearPreview()
        {
            if (Preview == null)
                return;
            Preview = null;
            _previewedRequest = null;
            Rows.Clear();
        }

        public RentalPromotionRequest? BuildRequest(out string? error)
        {
            error = null;
            if (DecimalInputConverter.Parse(PercentText) is not { } percent || percent != Math.Round(percent) || percent < 5 || percent > 90)
            {
                error = "La remise doit être un nombre entier entre 5 et 90 %.";
                return null;
            }
            if (EndDate.Date < StartDate.Date)
            {
                error = "La date de fin doit suivre la date de début.";
                return null;
            }

            return new RentalPromotionRequest
            {
                AdviceId = AdviceId > 0 ? AdviceId : null,
                ProductIds = AdviceId <= 0 && ProductId > 0 ? new List<int> { ProductId } : null,
                Percent = (int)percent,
                StartDate = StartDate.Date,
                EndDate = EndDate.Date,
                Weekdays = Weekdays.Where(d => d.IsSelected).Select(d => int.Parse(d.Code, CultureInfo.InvariantCulture)).ToList(),
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
                var preview = await _service.PreviewAsync(request);
                preview.Currency = SiteCurrency.CurrentCurrency;
                Preview = preview;
                _previewedRequest = request;
                Rows.Clear();
                foreach (var row in preview.Rows())
                    Rows.Add(row);
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
            if (Preview is not { CanApply: true, previewHash: { } hash } preview || _previewedRequest is not { } request || IsBusy)
                return;

            var confirmed = await _dialogService.ConfirmAsync(
                "Créer la promotion de location",
                $"{preview.PeriodText}\nLa remise compte selon le jour où la location commence, en boutique et en caisse. Vous pourrez la terminer à tout moment depuis Statistiques › Locations.",
                "Créer", "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                var result = await _service.ApplyAsync(request, hash);
                var count = result.batch?.promotions.Count ?? 0;
                RentalStatisticsViewModel.PromotionsChanged = true;
                await _dialogService.ShowInfoAsync(count > 1 ? $"Promotion créée sur {count} articles." : "Promotion de location créée.");
                if (Shell.Current is { } shell)
                    await shell.GoToAsync("..");
            }
            catch (ApiException ex) when (ex.ApiError.Code is "PRICES_CHANGED" or "PROMOTION_OVERLAP" or "NOT_RENTABLE")
            {
                var message = ex.ApiError.Code switch
                {
                    "PROMOTION_OVERLAP" => "Une promotion existe déjà sur ces jours pour un article. Vérifiez le nouvel aperçu.",
                    "NOT_RENTABLE" => "Un article ne se loue plus. Vérifiez le nouvel aperçu.",
                    _ => "Les tarifs ont changé depuis l'aperçu. Vérifiez le nouvel aperçu.",
                };
                await _dialogService.ShowInfoAsync(message);
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
}
