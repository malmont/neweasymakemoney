using System.Globalization;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Statistiques
{
    /// <summary>
    /// Statistiques › Locations (lot backend LOC1) : revenu à la date de vente et par date de location, occupation par article et par jour
    /// de la semaine, jours pleins et creux. Plage : période en cours (par défaut), 30 derniers jours, une période ou des dates.
    /// </summary>
    public partial class RentalStatisticsViewModel : BaseViewModel
    {
        private readonly IRentalStatisticsService _service;
        private readonly IListCollectionService _periods;
        private readonly IDialogService _dialogService;
        private int _requestVersion;
        private RentalQuery _query = new();
        private bool _loaded;

        public RentalStatisticsViewModel(IRentalStatisticsService service, IListCollectionService periods, IDialogService dialogService)
        {
            _service = service;
            _periods = periods;
            _dialogService = dialogService;
            Ranges = StatsChoice.List(("current", "Période en cours"), ("30d", "30 jours"), ("period", "Autre période"), ("custom", "Dates"));
            StatsChoice.Select(Ranges, "current");
        }

        public IReadOnlyList<StatsChoice> Ranges { get; }
        public ObservableCollection<RentalProductRow> Products { get; } = new();
        public ObservableCollection<ListCollection> Periods { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowsPeriodPicker), nameof(ShowsCustomDates))]
        private string _rangeCode = "current";

        public bool ShowsPeriodPicker => RangeCode == "period";
        public bool ShowsCustomDates => RangeCode == "custom";

        [ObservableProperty]
        private ListCollection? _selectedPeriod;

        [ObservableProperty]
        private DateTime _customFrom = DateTime.Today.AddDays(-29);

        [ObservableProperty]
        private DateTime _customTo = DateTime.Today;

        [ObservableProperty]
        private RentalStatistics? _data;

        [ObservableProperty]
        private bool _hasData;

        [ObservableProperty]
        private IList<StatsChartPoint> _weekdayPoints = new List<StatsChartPoint>();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedDayText))]
        private int _selectedDayIndex = -1;

        public string SelectedDayText => SelectedDayIndex >= 0 && SelectedDayIndex < WeekdayPoints.Count
            ? WeekdayPoints[SelectedDayIndex].Label
            : "Touchez un jour pour lire son occupation moyenne.";

        public async Task InitializeAsync()
        {
            if (_loaded)
                return;
            _loaded = true;
            await LoadAsync();
        }

        [RelayCommand]
        private async Task SelectRange(string code)
        {
            RangeCode = code;
            StatsChoice.Select(Ranges, code);
            switch (code)
            {
                case "current":
                    _query = new RentalQuery();
                    await LoadAsync();
                    break;
                case "30d":
                    _query = new RentalQuery(From: DateTime.Today.AddDays(-29), To: DateTime.Today);
                    await LoadAsync();
                    break;
                case "period":
                    await EnsurePeriodsAsync();
                    break; // le choix de la période lance le chargement
            }
        }

        partial void OnSelectedPeriodChanged(ListCollection? value)
        {
            if (value != null && RangeCode == "period")
            {
                _query = new RentalQuery(PeriodId: value.id);
                _ = LoadAsync();
            }
        }

        [RelayCommand]
        private async Task ApplyCustomDates()
        {
            if (CustomTo.Date < CustomFrom.Date)
            {
                await _dialogService.ShowInfoAsync("La date de fin doit suivre la date de début.");
                return;
            }
            if ((CustomTo.Date - CustomFrom.Date).TotalDays >= 366)
            {
                await _dialogService.ShowInfoAsync("Choisissez 366 jours au plus.");
                return;
            }
            _query = new RentalQuery(From: CustomFrom.Date, To: CustomTo.Date);
            await LoadAsync();
        }

        public async Task LoadAsync()
        {
            var version = ++_requestVersion;
            IsBusy = true;
            try
            {
                var data = await _service.GetAsync(_query);
                if (version != _requestVersion)
                    return;
                Data = data;
                HasData = true;
                Products.Clear();
                foreach (var row in data.ProductRows())
                    Products.Add(row);
                WeekdayPoints = data.WeekdayPoints();
                SelectedDayIndex = -1;
                OnPropertyChanged(nameof(SelectedDayText));
            }
            catch (Exception ex)
            {
                if (version != _requestVersion)
                    return;
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
            finally
            {
                if (version == _requestVersion)
                    IsBusy = false;
            }
        }

        private async Task EnsurePeriodsAsync()
        {
            if (Periods.Count > 0)
                return;
            try
            {
                var periods = await _periods.GetCollectionList();
                foreach (var period in periods.Where(p => !p.del).OrderByDescending(p => p.startDateCollection))
                    Periods.Add(period);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
        }
    }
}
