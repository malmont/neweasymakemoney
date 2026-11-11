using System.Globalization;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Statistiques
{
    /// <summary>
    /// Statistiques (lot backend S1) : cartes d'indicateurs comparées à la plage précédente et à l'an dernier, graphique avec l'an dernier,
    /// choix de la plage (aujourd'hui, 7 jours, mois, année, période de vente, dates libres) et du canal. Les écrans détaillés restent accessibles.
    /// </summary>
    public partial class StatistiquesViewModel : BaseViewModel
    {
        private readonly IUnifiedStatisticsService _statistics;
        private readonly IListCollectionService _periods;
        private readonly IDialogService _dialogService;
        private int _requestVersion;
        private UnifiedStatistics? _data;

        public StatistiquesViewModel(IUnifiedStatisticsService statistics, IListCollectionService periods, IDialogService dialogService)
        {
            _statistics = statistics;
            _periods = periods;
            _dialogService = dialogService;
            Ranges = StatsChoice.List(("today", "Aujourd'hui"), ("7d", "7 jours"), ("month", "Mois"), ("year", "Année"), ("period", "Période"), ("custom", "Dates"));
            Sources = StatsChoice.List(("all", "Tout"), ("ecommerce", "Boutique en ligne"), ("pos", "Caisse"), ("mobile_app", "Application"));
            Metrics = StatsChoice.List(("revenue", "Ventes"), ("grossMargin", "Marge"), ("orders", "Commandes"));
            StatsChoice.Select(Ranges, "month");
            StatsChoice.Select(Sources, "all");
            StatsChoice.Select(Metrics, "revenue");
        }

        public IReadOnlyList<StatsChoice> Ranges { get; }
        public IReadOnlyList<StatsChoice> Sources { get; }
        public IReadOnlyList<StatsChoice> Metrics { get; }

        public ObservableCollection<StatsCardRow> CardRows { get; } = new();
        public ObservableCollection<StatsOtherRevenueRow> OtherRevenue { get; } = new();
        public ObservableCollection<ListCollection> Periods { get; } = new();

        private string _range = "month";
        private string _source = "all";
        private string _metric = "revenue";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowsPeriodPicker), nameof(ShowsCustomDates))]
        private string _rangeCode = "month";

        public bool ShowsPeriodPicker => RangeCode == "period";
        public bool ShowsCustomDates => RangeCode == "custom";

        [ObservableProperty]
        private ListCollection? _selectedPeriod;

        [ObservableProperty]
        private DateTime _customFrom = DateTime.Today.AddDays(-29);

        [ObservableProperty]
        private DateTime _customTo = DateTime.Today;

        [ObservableProperty]
        private string _comparisonText = string.Empty;

        [ObservableProperty]
        private IList<StatsChartPoint> _chartPoints = new List<StatsChartPoint>();

        [ObservableProperty]
        private bool _chartIsMoney = true;

        [ObservableProperty]
        private string? _currency;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedPointText))]
        private int _selectedPointIndex = -1;

        [ObservableProperty]
        private string _caveatsText = string.Empty;

        [ObservableProperty]
        private bool _hasCaveats;

        [ObservableProperty]
        private bool _hasOtherRevenue;

        [ObservableProperty]
        private bool _hasData;

        /// <summary>La valeur exacte du point touché, avec l'an dernier.</summary>
        public string SelectedPointText
        {
            get
            {
                if (SelectedPointIndex < 0 || SelectedPointIndex >= ChartPoints.Count)
                    return "Touchez une barre pour lire sa valeur. En gris : l'an dernier aux mêmes dates.";
                var point = ChartPoints[SelectedPointIndex];
                var unit = ChartIsMoney ? "money" : "count";
                var text = $"{point.Label} : {StatsFormat.Value(point.Value, unit, Currency)}";
                return point.LastYear is { } last ? $"{text} · an dernier {StatsFormat.Value(last, unit, Currency)}" : text;
            }
        }

        private readonly StatistiquesPageLoaderState _loader = new();

        public Task InitializeAsync() => _loader.RunOnceAsync(LoadAsync);

        [RelayCommand]
        private async Task SelectRange(string code)
        {
            RangeCode = code;
            StatsChoice.Select(Ranges, code);
            if (code == "period")
            {
                await EnsurePeriodsAsync();
                SelectedPeriod ??= Periods.FirstOrDefault(p => Contains(p, DateTime.Today)) ?? Periods.FirstOrDefault();
                return; // le choix de la période lance le chargement
            }
            if (code == "custom")
                return; // « Afficher » lance le chargement
            _range = code;
            await LoadAsync();
        }

        [RelayCommand]
        private async Task SelectSource(string code)
        {
            _source = code;
            StatsChoice.Select(Sources, code);
            await LoadAsync();
        }

        [RelayCommand]
        private void SelectMetric(string code)
        {
            _metric = code;
            StatsChoice.Select(Metrics, code);
            ApplyChart();
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
            _range = "custom";
            await LoadAsync();
        }

        partial void OnSelectedPeriodChanged(ListCollection? value)
        {
            if (value != null && RangeCode == "period")
            {
                _range = "period";
                _ = LoadAsync();
            }
        }

        private StatsQuery Query() => _range switch
        {
            "period" when SelectedPeriod != null => new StatsQuery(null, PeriodId: SelectedPeriod.id, Source: _source),
            "custom" => new StatsQuery(null, From: CustomFrom.Date, To: CustomTo.Date, Source: _source),
            _ => new StatsQuery(_range, Source: _source),
        };

        public async Task LoadAsync()
        {
            var version = ++_requestVersion;
            IsBusy = true;
            try
            {
                var data = await _statistics.GetAsync(Query());
                if (version != _requestVersion)
                    return; // une demande plus récente est partie entre-temps
                Show(data);
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

        private void Show(UnifiedStatistics data)
        {
            _data = data;
            Currency = data.currency;
            ComparisonText = data.ComparisonText;
            CardRows.Clear();
            foreach (var row in StatsCardRow.Pair(data.Cards()))
                CardRows.Add(row);
            OtherRevenue.Clear();
            foreach (var row in data.OtherRevenueRows())
                OtherRevenue.Add(row);
            HasOtherRevenue = OtherRevenue.Count > 0;
            CaveatsText = data.CaveatsText;
            HasCaveats = data.HasCaveats;
            HasData = true;
            ApplyChart();
        }

        private void ApplyChart()
        {
            if (_data == null)
                return;
            ChartIsMoney = _metric != "orders";
            ChartPoints = _data.ChartPoints(_metric);
            SelectedPointIndex = -1;
            OnPropertyChanged(nameof(SelectedPointText));
        }

        /// <summary>La période en cours : celle qui contient aujourd'hui.</summary>
        public static bool Contains(ListCollection period, DateTime day) =>
            DateTimeOffset.TryParse(period.startDateCollection, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var start)
            && DateTimeOffset.TryParse(period.endDateCollection, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var end)
            && start.Date <= day.Date && day.Date <= end.Date;

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

        [RelayCommand]
        private async Task NavigateToYear()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(YearStatisticsPage));
        }

        [RelayCommand]
        private async Task NavigateToChiffreAffaire()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(ChiffresAffairePage));
        }

        [RelayCommand]
        private async Task NavigateToPurchasing()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(OrderNumberPage));
        }

        [RelayCommand]
        private async Task NavigateToPanier()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(PanierMoyenPage));
        }

        [RelayCommand]
        private async Task NavigateToStock()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(StockPage));
        }

        [RelayCommand]
        private async Task NavigateToFrais()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(FraisEvolutionEntreprisePage));
        }

        [RelayCommand]
        private async Task NavigateToCaisse()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(TransactionCaissePage));
        }

        [RelayCommand]
        private async Task NavigateToPayment()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(PaiementRemboursementPage));
        }

        [RelayCommand]
        private async Task NavigateToFraisTotal()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(FraisClientTotalViewPage));
        }
    }

    /// <summary>Un bouton de choix (plage, canal, indicateur) : sélectionné en couleur du thème.</summary>
    public partial class StatsChoice : ObservableObject
    {
        public StatsChoice(string code, string label)
        {
            Code = code;
            Label = label;
        }

        public string Code { get; }
        public string Label { get; }

        [ObservableProperty]
        private bool _isSelected;

        public static IReadOnlyList<StatsChoice> List(params (string Code, string Label)[] items) =>
            items.Select(item => new StatsChoice(item.Code, item.Label)).ToList();

        public static void Select(IEnumerable<StatsChoice> choices, string code)
        {
            foreach (var choice in choices)
                choice.IsSelected = choice.Code == code;
        }
    }

    /// <summary>Premier chargement une seule fois, même si OnAppearing revient (retour d'un écran détaillé).</summary>
    internal sealed class StatistiquesPageLoaderState
    {
        private bool _loaded;

        public async Task RunOnceAsync(Func<Task> load)
        {
            if (_loaded)
                return;
            _loaded = true;
            await load();
        }
    }
}
