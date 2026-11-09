
using Easymakemoney.Services.Dialog;
using Easymakemoney.UseCase.ChiffreAffaireUseCase;

namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class StatistiquesDataValueViewmodel : BaseViewModel
    {
        private readonly IDialogService? _dialogService;
        private readonly ChiffreAffaireUseCase? _chiffreAffaireUseCase;
        private readonly AverageOrderValueUseCase? _averageOrderValueUseCase;

        // Propriétés de recettes et périodes
        [ObservableProperty] private double currentDataValueWeek;
        [ObservableProperty] private double lastDataValueWeek;
        [ObservableProperty] private double currentDataValueMonth;
        [ObservableProperty] private double lastDataValueMonth;
        [ObservableProperty] private double currentDataValueYear;
        [ObservableProperty] private double lastDataValueYear;
        [ObservableProperty] private double currentRevenue;
        [ObservableProperty] private double lastDataValue;
        [ObservableProperty] private string currentPeriode;
        [ObservableProperty] private string lastPeriode;
        [ObservableProperty] private string typeDataValue;

        // Collections de données pour différentes périodes
        [ObservableProperty] private List<IStatistiqueData> dailyDataValueCurrentWeek;
        [ObservableProperty] private List<IStatistiqueData> weeklyDataValueForCurrentMonth;
        [ObservableProperty] private List<IStatistiqueData> monthlyDataValueForCurrentYear;
        [ObservableProperty] private List<IStatistiqueData>? allDataValue;

        public int periodeTypeId { get; set; }

        // Propriétés pour chaque graphique
        [ObservableProperty] private Chart weekChart;
        [ObservableProperty] private Chart monthChart;
        [ObservableProperty] private Chart yearChart;

        [ObservableProperty] bool isWeekChartVisible;

        [ObservableProperty] bool isMonthChartVisible;

        [ObservableProperty] bool isYearChartVisible;
          public List<PeriodeType> PeriodeTypeChoices { get; }

        public StatistiquesDataValueViewmodel(IDialogService? dialogService = null,
            ChiffreAffaireUseCase? chiffreAffaireUseCase = null,
            AverageOrderValueUseCase? averageOrderValueUseCase = null)
        {
            _dialogService = dialogService;
            _chiffreAffaireUseCase = chiffreAffaireUseCase;
            _averageOrderValueUseCase = averageOrderValueUseCase;
            PeriodeTypeChoices = StaticData.StaticData.PeriodeTypeChoices;
        }

        public async Task LoadRevenueStatisticsAsync()
        {
            if (_chiffreAffaireUseCase == null) return;
            try
            {
                var data = await _chiffreAffaireUseCase.GetRevenueStatistics();
                LoadDataChiffreAffaire(data);
            }
            catch (Exception ex)
            {
                if (_dialogService != null)
                    await _dialogService.ShowErrorAsync(ex);
            }
        }

        public async Task LoadAverageOrderValueStatisticsAsync()
        {
            if (_averageOrderValueUseCase == null) return;
            try
            {
                var data = await _averageOrderValueUseCase.GetAverageOrderValueStatistics();
                LoadDataChiffreAffaire(data);
            }
            catch (Exception ex)
            {
                if (_dialogService != null)
                    await _dialogService.ShowErrorAsync(ex);
            }
        }
        public void LoadDataChiffreAffaire<TDaily, TWeekly, TMonthly>(IAllDataStatistics<TDaily, TWeekly, TMonthly>? data)
            where TDaily : IStatistiqueData
            where TWeekly : IStatistiqueData
            where TMonthly : IStatistiqueData
        {
            if (data == null)
                return;

            CurrentPeriode = "Semaine en cours";
            LastPeriode = "Semaine précédente";
            TypeDataValue = "Recette journalière";

            // Utilisation des propriétés de l'interface IAllDataStatistics
            CurrentDataValueWeek = data.CurrentWeekData;
            LastDataValueWeek = data.LastWeekData;
            CurrentDataValueMonth = data.CurrentMonthData;
            LastDataValueMonth = data.LastMonthData;
            CurrentDataValueYear = data.CurrentYearData;
            LastDataValueYear = data.LastYearData;

            DailyDataValueCurrentWeek = data.DailyDataForCurrentWeek != null
                ? data.DailyDataForCurrentWeek.Cast<IStatistiqueData>().ToList()
                : new List<IStatistiqueData>();
            WeeklyDataValueForCurrentMonth = data.WeeklyDataForCurrentMonth != null
                ? data.WeeklyDataForCurrentMonth.Cast<IStatistiqueData>().ToList()
                : new List<IStatistiqueData>();
            MonthlyDataValueForCurrentYear = data.MonthlyDataForCurrentYear != null
                ? data.MonthlyDataForCurrentYear.Cast<IStatistiqueData>().ToList()
                : new List<IStatistiqueData>();

            // Appliquer les couleurs alternées
            ApplyAlternateRowColors(DailyDataValueCurrentWeek);
            ApplyAlternateRowColors(WeeklyDataValueForCurrentMonth);
            ApplyAlternateRowColors(MonthlyDataValueForCurrentYear);
        }

        public void UpdateRevenueData(int periodeTypeId)
        {

            IsWeekChartVisible = false;
            IsMonthChartVisible = false;
            IsYearChartVisible = false;
            // Assurez-vous de créer des graphiques distincts pour chaque période
            switch (periodeTypeId)
            {
                case 1: // Semaine
                    AllDataValue = dailyDataValueCurrentWeek;
                    CurrentRevenue = currentDataValueWeek;
                    LastDataValue = lastDataValueWeek;
                    CurrentPeriode = "Semaine en cours";
                    LastPeriode = "Semaine précédente";
                    TypeDataValue = "Recette journalière";
                    WeekChart = ChartGenerator.GenerateChart(AllDataValue); 
                    IsWeekChartVisible = true;
                    break;

                case 2: // Mois
                    AllDataValue = weeklyDataValueForCurrentMonth;
                    CurrentRevenue = currentDataValueMonth;
                    LastDataValue = lastDataValueMonth;
                    CurrentPeriode = "Mois en cours";
                    LastPeriode = "Mois précédent";
                    TypeDataValue = "Recette mensuelle";
                    MonthChart = ChartGenerator.GenerateChart(AllDataValue); 
                    IsMonthChartVisible = true;
                    break;

                case 3: // Année
                    AllDataValue = monthlyDataValueForCurrentYear;
                    CurrentRevenue = currentDataValueYear;
                    LastDataValue = lastDataValueYear;
                    CurrentPeriode = "Année en cours";
                    LastPeriode = "Année précédente";
                    TypeDataValue = "Recette annuelle";
                    YearChart = ChartGenerator.GenerateChart(AllDataValue); 
                    IsYearChartVisible = true;
                    break;
            }
        }

        [RelayCommand]
        public async Task ShowBottomSheetPopupListView()
        {
            var viewModel = new BottomSheetPopupListViewViewModel(
                getItemsFunc: async () => await Task.FromResult(PeriodeTypeChoices),
                onItemTappedAction: async (item) =>
                {
                    if (item is PeriodeType periodeType)
                    {
                        periodeTypeId = periodeType.id;
                        UpdateRevenueData(periodeTypeId);
                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.ShowPopupAsync(popup);
        }

        private void ApplyAlternateRowColors(List<IStatistiqueData> dataList)
        {
            for (int i = 0; i < dataList.Count; i++)
            {
                dataList[i].BackgroundColor = i % 2 == 0
                    ? Colors.LightGray.WithAlpha(0.3f)
                    : Colors.Transparent;
            }
        }
    }
}
