using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Statistiques
{
    /// <summary>
    /// Statistiques › Année (lots backend AN + EX) : l'exercice de l'entreprise mois par mois, son résultat (marge + locations et abonnements
    /// − frais variables − frais fixes), la fin d'exercice estimée, le point mort, ce qui manque et les leviers chiffrés ; pour un exercice
    /// terminé, le bilan, « ce qui aurait suffi » et l'avis du guide.
    /// </summary>
    public partial class YearStatisticsViewModel : BaseViewModel
    {
        private readonly IYearStatisticsService _service;
        private readonly IDialogService _dialogService;
        private int _requestVersion;
        private int? _currentStartYear;
        private int? _year;
        private int? _pricePercent;
        private string _metric = "result";

        /// <summary>Mis à vrai quand le mois de début de l'exercice change : l'écran se recharge à son retour.</summary>
        public static bool FiscalYearChanged { get; set; }

        public YearStatisticsViewModel(IYearStatisticsService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
            Metrics = StatsChoice.List(("result", "Résultat"), ("sales", "Ventes"), ("grossMargin", "Marge"), ("cumulative", "Cumul"));
            PriceChoices = StatsChoice.List(("3", "+3 %"), ("5", "+5 %"), ("10", "+10 %"), ("15", "+15 %"));
            StatsChoice.Select(Metrics, "result");
            StatsChoice.Select(PriceChoices, "5");
        }

        public IReadOnlyList<StatsChoice> Metrics { get; }
        public IReadOnlyList<StatsChoice> PriceChoices { get; }

        public ObservableCollection<YearRow> Composition { get; } = new();
        public ObservableCollection<YearLeverRow> Levers { get; } = new();
        public ObservableCollection<YearPeriodRow> Periods { get; } = new();

        [ObservableProperty]
        private YearStatistics? _data;

        [ObservableProperty]
        private bool _hasData;

        [ObservableProperty]
        private bool _canGoBack;

        [ObservableProperty]
        private bool _canGoForward;

        [ObservableProperty]
        private IList<StatsChartPoint> _chartPoints = new List<StatsChartPoint>();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedPointText))]
        private int _selectedPointIndex = -1;

        [ObservableProperty]
        private bool _hasLevers;

        [ObservableProperty]
        private bool _hasPeriods;

        [ObservableProperty]
        private string _fiscalYearText = string.Empty;

        [ObservableProperty]
        private YearComment? _comment;

        [ObservableProperty]
        private bool _hasComment;

        [ObservableProperty]
        private bool _isCommentBusy;

        public string SelectedPointText
        {
            get
            {
                if (SelectedPointIndex < 0 || SelectedPointIndex >= ChartPoints.Count)
                    return "Touchez un mois pour lire sa valeur. Barres claires : mois estimés. En gris : l'exercice précédent.";
                var point = ChartPoints[SelectedPointIndex];
                var text = $"{point.Label} : {Data?.Money((long)point.Value)}";
                return point.LastYear is { } last ? $"{text} · exercice précédent {Data?.Money((long)last)}" : text;
            }
        }

        private bool _loaded;

        public async Task InitializeAsync()
        {
            if (_loaded && !FiscalYearChanged)
                return;
            _loaded = true;
            if (FiscalYearChanged)
            {
                FiscalYearChanged = false;
                _year = null; // l'exercice en cours, avec le nouveau mois de début
                _currentStartYear = null;
            }
            await LoadAsync();
        }

        [RelayCommand]
        private async Task PreviousYear()
        {
            if (Data == null || Data.year <= 2000)
                return;
            _year = Data.year - 1;
            await LoadAsync();
        }

        [RelayCommand]
        private async Task NextYear()
        {
            if (Data == null || _currentStartYear is not { } current || Data.year >= current)
                return;
            _year = Data.year + 1;
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
        private async Task SelectPrice(string code)
        {
            _pricePercent = int.Parse(code, System.Globalization.CultureInfo.InvariantCulture);
            StatsChoice.Select(PriceChoices, code);
            await LoadAsync();
        }

        [RelayCommand]
        private async Task AskComment()
        {
            if (Data is not { IsFinished: true } data || IsCommentBusy)
                return;
            IsCommentBusy = true;
            try
            {
                Comment = await _service.GetCommentAsync(data.year);
                HasComment = Comment?.comment != null;
            }
            catch (ApiException ex) when (ex.ApiError.Code == "COMMENT_LIMIT")
            {
                await _dialogService.ShowInfoAsync("Le guide a déjà donné 10 avis aujourd'hui. Réessayez demain.", "Avis du guide");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
            finally
            {
                IsCommentBusy = false;
            }
        }

        [RelayCommand]
        private async Task OpenFiscalYear()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(FiscalYearPage));
        }

        public async Task LoadAsync()
        {
            var version = ++_requestVersion;
            IsBusy = true;
            try
            {
                var data = await _service.GetAsync(_year, _pricePercent);
                if (version != _requestVersion)
                    return;
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

        private void Show(YearStatistics data)
        {
            _currentStartYear ??= data.year; // premier chargement : l'exercice en cours
            Data = data;
            HasData = true;
            CanGoBack = data.year > 2000;
            CanGoForward = data.year < _currentStartYear;
            FiscalYearText = FiscalYearSettings.RangeText(data.fiscalYear?.startMonth ?? 1);

            Composition.Clear();
            foreach (var row in data.CompositionRows())
                Composition.Add(row);
            Levers.Clear();
            foreach (var lever in data.IsFinished ? new List<YearLeverRow>() : data.LeverRows())
                Levers.Add(lever);
            HasLevers = Levers.Count > 0;
            Periods.Clear();
            foreach (var period in data.PeriodRows())
                Periods.Add(period);
            HasPeriods = Periods.Count > 0;
            Comment = null;
            HasComment = false;
            ApplyChart();
        }

        private void ApplyChart()
        {
            if (Data == null)
                return;
            ChartPoints = Data.ChartPoints(_metric);
            SelectedPointIndex = -1;
            OnPropertyChanged(nameof(SelectedPointText));
        }
    }

    /// <summary>Gestion › Exercice comptable : le mois de début de l'exercice du site.</summary>
    public partial class FiscalYearViewModel : BaseViewModel
    {
        private readonly IYearStatisticsService _service;
        private readonly IDialogService _dialogService;
        private int _savedMonth = 1;

        public FiscalYearViewModel(IYearStatisticsService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
        }

        public IReadOnlyList<TreasuryChoice> Months => FiscalYearSettings.Months;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PreviewText), nameof(AlignmentText), nameof(IsChanged))]
        private TreasuryChoice? _selectedMonth;

        [ObservableProperty]
        private string _currentText = string.Empty;

        public string PreviewText => SelectedMonth == null ? string.Empty : FiscalYearSettings.RangeText(int.Parse(SelectedMonth.Code));

        public string AlignmentText => SelectedMonth == null ? string.Empty : FiscalYearSettings.AlignmentText(int.Parse(SelectedMonth.Code));

        public bool IsChanged => SelectedMonth != null && int.Parse(SelectedMonth.Code) != _savedMonth;

        private bool _loaded;

        public async Task InitializeAsync()
        {
            if (_loaded)
                return;
            _loaded = true;
            try
            {
                Apply(await _service.GetFiscalYearAsync());
            }
            catch (Exception ex)
            {
                _loaded = false;
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
        }

        private void Apply(FiscalYearSetting setting)
        {
            _savedMonth = setting.startMonth is >= 1 and <= 12 ? setting.startMonth : 1;
            SelectedMonth = Months[_savedMonth - 1];
            CurrentText = setting.current is { } current
                ? $"Exercice en cours : {current.label}, du {TreasuryDates.Short(current.from)} au {TreasuryDates.Short(current.to)}"
                : string.Empty;
            OnPropertyChanged(nameof(IsChanged));
        }

        [RelayCommand]
        private async Task Save()
        {
            if (SelectedMonth == null || IsBusy || !IsChanged)
                return;
            var month = int.Parse(SelectedMonth.Code);
            var confirmed = await _dialogService.ConfirmAsync(
                "Changer l'exercice",
                $"L'exercice ira {FiscalYearSettings.RangeText(month).ToLowerInvariant()}. Les statistiques de l'année seront recalculées ; rien n'est effacé.",
                "Changer", "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                Apply(await _service.SaveFiscalYearAsync(month));
                YearStatisticsViewModel.FiscalYearChanged = true;
                await _dialogService.ShowInfoAsync("L'exercice est enregistré.", "Exercice comptable");
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
