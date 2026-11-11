using System.Globalization;
using Easymakemoney.Converters;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;
using Easymakemoney.Utilities;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Préparer une période entière : ventes visées, achats de stock, voyage, autres frais ; simulation, avis du guide,
    /// puis « Valider » crée le budget de chaque période concernée et le voyage (seule écriture, après confirmation).
    /// </summary>
    public partial class PreparationViewModel : BaseViewModel
    {
        public sealed record Choice(string Code, string Label)
        {
            public override string ToString() => Label;
        }

        private readonly IPreparationService _service;
        private readonly IPeriodService _periodService;
        private readonly IDialogService _dialogService;
        private PreparationRequest? _simulatedRequest;

        public PreparationViewModel(IPreparationService service, IPeriodService periodService, IDialogService dialogService)
        {
            _service = service;
            _periodService = periodService;
            _dialogService = dialogService;
            // Champ direct : le chargement part de StartAsync, pas du constructeur.
            _selectedTarget = Targets[1];
        }

        public IReadOnlyList<Choice> Targets { get; } = new[]
        {
            new Choice("current", "La période en cours"),
            new Choice("next", "La période suivante"),
        };

        public ObservableCollection<Choice> SalesMethods { get; } = new();

        [ObservableProperty]
        private Choice? _selectedTarget;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCustomSales))]
        private Choice? _selectedSalesMethod;

        public bool IsCustomSales => SelectedSalesMethod?.Code == "custom";

        [ObservableProperty]
        private string? _customSalesText;

        [ObservableProperty]
        private string? _periodTitle;

        [ObservableProperty]
        private string? _recommendationText;

        private string? _targetDate;
        private PreparationEstimates? _estimates;

        // Achats prévus
        public ObservableCollection<PreparationPurchase> Purchases { get; } = new();

        [ObservableProperty]
        private DateTime _purchaseDate = DateTime.Today;

        [ObservableProperty]
        private string? _purchaseAmountText;

        [ObservableProperty]
        private string? _purchaseCoefficientText;

        public DateTime Today => DateTime.Today;

        // Voyage
        [ObservableProperty]
        private bool _hasTrip;

        [ObservableProperty]
        private DateTime _tripStart = DateTime.Today.AddDays(7);

        [ObservableProperty]
        private DateTime _tripEnd = DateTime.Today.AddDays(11);

        [ObservableProperty]
        private string? _tripPlace;

        [ObservableProperty]
        private string? _tripBudgetText;

        // Autres frais
        [ObservableProperty]
        private string? _shippingText;

        [ObservableProperty]
        private string? _advertisingText;

        [ObservableProperty]
        private string? _otherText;

        // Résultat
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSimulation))]
        private PreparationSimulation? _simulation;

        public bool HasSimulation => Simulation != null;

        public ObservableCollection<SimulationRow> Rows { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasAdvice))]
        private PreparationAdvice? _advice;

        public bool HasAdvice => Advice != null;

        public ObservableCollection<PreparationCandidate> Candidates { get; } = new();

        [ObservableProperty]
        private bool _isAdvising;

        public async Task StartAsync()
        {
            if (_estimates == null)
                await LoadEstimatesAsync();
        }

        partial void OnSelectedTargetChanged(Choice? value) => _ = LoadEstimatesAsync();

        /// <summary>La période visée (en cours ou suivante) et les trois estimations de ses ventes.</summary>
        private async Task LoadEstimatesAsync()
        {
            if (SelectedTarget == null)
                return;

            IsBusy = true;
            try
            {
                var date = DateTime.Today;
                if (SelectedTarget.Code == "next")
                {
                    var current = await _periodService.GetCurrentPeriodAsync();
                    if (current != null && DateTime.TryParse(current.endDateCollection, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
                        date = end.Date.AddDays(1);
                }

                _targetDate = TreasuryDates.Iso(date);
                var estimates = await _service.GetEstimatesAsync(_targetDate);
                _estimates = estimates;
                PeriodTitle = estimates.period == null
                    ? null
                    : $"{estimates.period.name} : du {TreasuryDates.Short(estimates.period.start)} au {TreasuryDates.Short(estimates.period.end)} ({estimates.days} jours)";

                SalesMethods.Clear();
                AddMethod("lastYear", "Même période l'an dernier", estimates.LastYearRevenue);
                AddMethod("average", "Moyenne des dernières périodes", estimates.AverageRevenue);
                AddMethod("pace", "Rythme des 30 derniers jours", estimates.PaceRevenue);
                SalesMethods.Add(new Choice("custom", "Mon objectif"));
                SelectedSalesMethod = SalesMethods.FirstOrDefault(m => m.Code == estimates.recommended?.method) ?? SalesMethods.FirstOrDefault();
                RecommendationText = estimates.recommended?.reason;
                ClearResult();
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

        private void AddMethod(string code, string label, long? revenue)
        {
            if (revenue is { } value)
                SalesMethods.Add(new Choice(code, $"{label} : {SiteCurrency.FormatCents(value)}"));
        }

        [RelayCommand]
        private async Task AddPurchase()
        {
            if (DecimalInputConverter.Parse(PurchaseAmountText) is not { } amount || amount <= 0)
            {
                await _dialogService.ShowInfoAsync("Saisissez le montant de l'achat prévu.");
                return;
            }

            double? coefficient = null;
            if (!string.IsNullOrWhiteSpace(PurchaseCoefficientText))
            {
                coefficient = DecimalInputConverter.Parse(PurchaseCoefficientText);
                if (coefficient is null or < 1 or > 20)
                {
                    await _dialogService.ShowInfoAsync("Le coefficient doit être entre 1 et 20. Laissez-le vide pour celui du site.");
                    return;
                }
            }

            Purchases.Add(new PreparationPurchase { Date = PurchaseDate.Date, AmountCents = MoneyHelper.ToCents(amount), Coefficient = coefficient });
            PurchaseAmountText = null;
            PurchaseCoefficientText = null;
            ClearResult();
        }

        [RelayCommand]
        private void RemovePurchase(PreparationPurchase? purchase)
        {
            if (purchase != null && Purchases.Remove(purchase))
                ClearResult();
        }

        private PreparationRequest? BuildRequest(out string? error)
        {
            error = null;
            long? custom = null;
            if (IsCustomSales)
            {
                if (DecimalInputConverter.Parse(CustomSalesText) is not { } sales || sales <= 0)
                {
                    error = "Saisissez votre objectif de ventes pour la période.";
                    return null;
                }
                custom = MoneyHelper.ToCents(sales);
            }

            PreparationTrip? trip = null;
            if (HasTrip)
            {
                if (TripEnd.Date < TripStart.Date)
                {
                    error = "La fin du voyage doit suivre son début.";
                    return null;
                }
                if (DecimalInputConverter.Parse(TripBudgetText) is not { } budget || budget <= 0)
                {
                    error = "Saisissez le budget total du voyage (frais : déplacement, hôtel, repas).";
                    return null;
                }
                trip = new PreparationTrip { StartDate = TripStart.Date, EndDate = TripEnd.Date, Place = TripPlace, BudgetTotalCents = MoneyHelper.ToCents(budget) };
            }

            return new PreparationRequest
            {
                Date = _targetDate,
                SalesMethod = SelectedSalesMethod?.Code ?? "lastYear",
                SalesAmount = custom,
                Purchases = Purchases.ToList(),
                Trip = trip,
                Shipping = Cents(ShippingText),
                Advertising = Cents(AdvertisingText),
                Other = Cents(OtherText),
            };
        }

        private static long? Cents(string? text) =>
            DecimalInputConverter.Parse(text) is { } value && value > 0 ? MoneyHelper.ToCents(value) : null;

        [RelayCommand]
        private async Task Simulate()
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
                var simulation = await _service.SimulateAsync(request);
                Simulation = simulation;
                _simulatedRequest = request;
                Advice = null;
                Candidates.Clear();
                Rows.Clear();
                foreach (var row in simulation.Rows())
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
        private async Task AskGuide()
        {
            if (_simulatedRequest == null || IsAdvising)
                return;

            IsAdvising = true;
            try
            {
                var advice = await _service.AdviseAsync(_simulatedRequest);
                Advice = advice;
                Candidates.Clear();
                foreach (var candidate in advice.candidates.OrderByDescending(c => c.IsRecommended))
                    Candidates.Add(candidate);
            }
            catch (ApiException ex) when (ex.ApiError.Code == "COMMENT_LIMIT")
            {
                await _dialogService.ShowInfoAsync("Le guide a déjà donné 10 avis aujourd'hui. Réessayez demain.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
            finally
            {
                IsAdvising = false;
            }
        }

        [RelayCommand]
        private async Task ShowHelp(SimulationRow? row)
        {
            if (row != null && !string.IsNullOrEmpty(row.Help))
                await _dialogService.ShowInfoAsync(row.Help, row.Label);
        }

        /// <summary>Crée le budget de chaque période concernée, et le voyage s'il y en a un. Tout ou rien.</summary>
        [RelayCommand]
        private async Task Validate()
        {
            if (Simulation?.previewHash is not { } hash || _simulatedRequest is not { } request || IsBusy)
                return;

            var confirmed = await _dialogService.ConfirmAsync(
                "Valider ce plan",
                $"Le budget suivant sera enregistré :\n{Simulation.PlannedText}" + (request.Trip != null ? "\nLe voyage d'achat sera créé." : string.Empty),
                "Valider", "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                await _service.ValidateAsync(request, hash);
                await _dialogService.ShowInfoAsync("Plan validé : le budget est enregistré" + (request.Trip != null ? " et le voyage créé." : "."));
                if (Shell.Current is { } shell)
                    await shell.GoToAsync("..");
            }
            catch (ApiException ex) when (ex.ApiError.Code == "PRICES_CHANGED")
            {
                await _dialogService.ShowInfoAsync("Le stock, les coûts ou la trésorerie ont changé depuis le calcul. Vérifiez le nouveau calcul avant de valider.");
                IsBusy = false;
                await Simulate();
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

        partial void OnSelectedSalesMethodChanged(Choice? value) => ClearResult();
        partial void OnHasTripChanged(bool value) => ClearResult();

        private void ClearResult()
        {
            Simulation = null;
            _simulatedRequest = null;
            Advice = null;
            Rows.Clear();
            Candidates.Clear();
        }
    }
}
