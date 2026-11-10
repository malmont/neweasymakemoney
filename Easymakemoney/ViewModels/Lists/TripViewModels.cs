using System.Globalization;
using Easymakemoney.Converters;
using Easymakemoney.Services.Dialog;
using Easymakemoney.Utilities;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>Liste des voyages d'achat, les plus récents d'abord.</summary>
    public partial class TripListViewModel : BaseViewModel
    {
        private readonly ITripService _tripService;
        private readonly IDialogService _dialogService;

        public TripListViewModel(ITripService tripService, IDialogService dialogService)
        {
            _tripService = tripService;
            _dialogService = dialogService;
        }

        public ObservableCollection<Trip> Trips { get; } = new();

        [ObservableProperty]
        private bool _isEmpty;

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                var trips = await _tripService.GetTripsAsync();
                Trips.Clear();
                foreach (var trip in trips)
                    Trips.Add(trip);
                IsEmpty = Trips.Count == 0;
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
        private async Task NewTrip()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(TripFormPage));
        }

        [RelayCommand]
        private async Task Open(Trip? trip)
        {
            if (trip != null && Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(TripFollowUpPage)}?TripId={trip.id}");
        }
    }

    /// <summary>Suivi d'un voyage : dépensé contre budget, jour par jour.</summary>
    public partial class TripFollowUpViewModel : BaseViewModel
    {
        private readonly ITripService _tripService;
        private readonly IDialogService _dialogService;

        public TripFollowUpViewModel(ITripService tripService, IDialogService dialogService)
        {
            _tripService = tripService;
            _dialogService = dialogService;
        }

        public int TripId { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFollowUp))]
        private TripFollowUp? _followUp;

        public bool HasFollowUp => FollowUp != null;

        [ObservableProperty]
        private Trip? _trip;

        public ObservableCollection<TripDay> Days { get; } = new();

        public async Task LoadAsync()
        {
            if (TripId <= 0 || IsBusy)
                return;

            IsBusy = true;
            try
            {
                var tripTask = _tripService.GetTripAsync(TripId);
                var followUpTask = _tripService.GetFollowUpAsync(TripId);
                await Task.WhenAll(tripTask, followUpTask);
                Trip = tripTask.Result;
                FollowUp = followUpTask.Result;
                Days.Clear();
                foreach (var day in FollowUp.SpendingDays())
                    Days.Add(day);
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
        private async Task Edit()
        {
            if (TripId > 0 && Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(TripFormPage)}?TripId={TripId}");
        }

        [RelayCommand]
        private async Task Delete()
        {
            if (TripId <= 0 || IsBusy)
                return;

            var confirmed = await _dialogService.ConfirmAsync(
                "Supprimer le voyage",
                "Les notes de frais et commandes rattachées restent enregistrées, elles ne sont simplement plus liées à ce voyage.",
                "Supprimer", "Annuler");
            if (!confirmed)
                return;

            try
            {
                await _tripService.DeleteAsync(TripId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
                return;
            }

            if (Shell.Current is { } shell)
                await shell.GoToAsync("..");
        }
    }

    /// <summary>Création ou modification d'un voyage d'achat (dates, lieu, budget total et par jour, devise).</summary>
    public partial class TripFormViewModel : BaseViewModel
    {
        public const string SiteCurrencyChoice = "Devise du site";

        private readonly ITripService _tripService;
        private readonly IDialogService _dialogService;
        private string? _siteCurrency;

        public TripFormViewModel(ITripService tripService, IDialogService dialogService)
        {
            _tripService = tripService;
            _dialogService = dialogService;
            _siteCurrency = SiteCurrency.CurrentCurrency;
        }

        public int TripId { get; private set; }
        public bool IsEdit => TripId > 0;
        public string PageTitle => IsEdit ? "Modifier le voyage" : "Nouveau voyage";

        public ObservableCollection<string> CurrencyChoices { get; } = new() { SiteCurrencyChoice, "CAD", "EUR", "USD" };

        [ObservableProperty]
        private string? _name;

        [ObservableProperty]
        private string? _place;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DaysText))]
        private DateTime _startDate = DateTime.Today;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DaysText))]
        private DateTime _endDate = DateTime.Today.AddDays(6);

        public string DaysText => EndDate.Date >= StartDate.Date ? $"{(EndDate.Date - StartDate.Date).Days + 1} jours" : "Dates à corriger";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NeedsExchangeRate))]
        [NotifyPropertyChangedFor(nameof(ExchangeRateLabel))]
        [NotifyPropertyChangedFor(nameof(BudgetLabel))]
        private string _selectedCurrency = SiteCurrencyChoice;

        public string? CurrencyCode => SelectedCurrency == SiteCurrencyChoice ? null : SelectedCurrency;
        public bool NeedsExchangeRate => CampaignRequestBuilder.NeedsExchangeRate(CurrencyCode, _siteCurrency);
        public string ExchangeRateLabel => NeedsExchangeRate ? CampaignRequestBuilder.ExchangeRateLabel(CurrencyCode, _siteCurrency) : string.Empty;
        public string BudgetLabel => CurrencyCode == null ? "Budget total" : $"Budget total ({CurrencyCode})";

        [ObservableProperty]
        private string? _exchangeRateText;

        [ObservableProperty]
        private string? _budgetTotalText;

        [ObservableProperty]
        private string? _budgetDailyText;

        [ObservableProperty]
        private string? _notes;

        public async Task LoadAsync(int tripId)
        {
            TripId = tripId;
            OnPropertyChanged(nameof(IsEdit));
            OnPropertyChanged(nameof(PageTitle));
            if (tripId <= 0)
                return;

            IsBusy = true;
            try
            {
                var trip = await _tripService.GetTripAsync(tripId);
                if (!string.IsNullOrWhiteSpace(trip.siteCurrency))
                    _siteCurrency = trip.siteCurrency.Trim().ToUpperInvariant();
                Name = trip.name;
                Place = trip.place;
                StartDate = TreasuryDates.Parse(trip.startDate) ?? DateTime.Today;
                EndDate = TreasuryDates.Parse(trip.endDate) ?? StartDate;
                var foreign = trip.IsForeign ? trip.currency!.ToUpperInvariant() : null;
                if (foreign != null && !CurrencyChoices.Contains(foreign))
                    CurrencyChoices.Add(foreign);
                SelectedCurrency = foreign ?? SiteCurrencyChoice;
                ExchangeRateText = trip.IsForeign && trip.exchangeRate is { } rate ? rate.ToString("0.####", CultureInfo.CurrentCulture) : null;
                BudgetTotalText = (trip.budgetTotal / 100.0).ToString("0.##", CultureInfo.CurrentCulture);
                BudgetDailyText = trip.budgetDailyEntered ? (trip.budgetDaily / 100.0).ToString("0.##", CultureInfo.CurrentCulture) : null;
                Notes = trip.notes;
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
        private async Task Save()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(Name))
            {
                await _dialogService.ShowInfoAsync("Donnez un nom au voyage (par exemple « Salon de Paris »).");
                return;
            }
            if (EndDate.Date < StartDate.Date)
            {
                await _dialogService.ShowInfoAsync("La date de fin doit suivre la date de début.");
                return;
            }
            if (DecimalInputConverter.Parse(BudgetTotalText) is not { } total || total <= 0)
            {
                await _dialogService.ShowInfoAsync("Saisissez le budget total du voyage.");
                return;
            }

            double? daily = null;
            if (!string.IsNullOrWhiteSpace(BudgetDailyText))
            {
                daily = DecimalInputConverter.Parse(BudgetDailyText);
                if (daily is null or <= 0)
                {
                    await _dialogService.ShowInfoAsync("Budget par jour invalide. Laissez-le vide pour le calculer.");
                    return;
                }
            }

            double? rate = null;
            if (NeedsExchangeRate)
            {
                rate = DecimalInputConverter.Parse(ExchangeRateText);
                if (rate is null or <= 0)
                {
                    await _dialogService.ShowInfoAsync($"Saisissez le taux de change ({ExchangeRateLabel}).");
                    return;
                }
            }

            var request = new TripRequest
            {
                name = Name.Trim(),
                place = string.IsNullOrWhiteSpace(Place) ? null : Place.Trim(),
                startDate = TreasuryDates.Iso(StartDate),
                endDate = TreasuryDates.Iso(EndDate),
                budgetTotal = MoneyHelper.ToCents(total),
                budgetDaily = daily.HasValue ? MoneyHelper.ToCents(daily) : null,
                currency = NeedsExchangeRate ? CurrencyCode : null,
                exchangeRate = rate,
                notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            };

            IsBusy = true;
            try
            {
                if (IsEdit)
                    await _tripService.UpdateAsync(TripId, request);
                else
                    await _tripService.CreateAsync(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
                return;
            }
            finally
            {
                IsBusy = false;
            }

            if (Shell.Current is { } shell)
                await shell.GoToAsync("..");
        }
    }
}
