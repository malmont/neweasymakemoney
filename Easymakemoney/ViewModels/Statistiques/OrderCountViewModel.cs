
using Easymakemoney.Services.Dialog;
using Easymakemoney.UseCase.OrderCountUseCase;

namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class OrderCountViewModel : BaseViewModel
    {
        private readonly IDialogService? _dialogService;
        private readonly OrderCountUseCase? _orderCountUseCase;

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
        [ObservableProperty] private double currentWeekOrderDeliveryValue;
        [ObservableProperty] private double lastWeekOrderDeliveryValue;
        [ObservableProperty] private double currentWeekOrderCancellationValue;
        [ObservableProperty] private double lastWeekOrderCancellationValue;
        [ObservableProperty] private double currentWeekReturnOrderCompleteValue;
        [ObservableProperty] private double lastWeekReturnOrderCompleteValue;




        [ObservableProperty] private double currentWeekOrderDelivery;
        [ObservableProperty] private double lastWeekOrderDelivery;

        [ObservableProperty] private double currentWeekOrderCancellation;
        [ObservableProperty] private double lastWeekOrderCancellation;

        [ObservableProperty] private double currentWeekReturnOrderComplete;

        [ObservableProperty] private double lastWeekReturnOrderComplete;

        // Collections de données pour différentes périodes
        [ObservableProperty] private List<IStatistiqueData> dailyDataValueCurrentWeek;
        [ObservableProperty] private List<IStatistiqueData> weeklyDataValueForCurrentMonth;
        [ObservableProperty] private List<IStatistiqueData> monthlyDataValueForCurrentYear;
        [ObservableProperty] private List<IStatistiqueData>? allDataValue;



        [ObservableProperty] private List<IStatistiqueData> dailycurrentWeekOrderDelivery;
        [ObservableProperty] private List<IStatistiqueData> dailyLcurrentWeekOrderCancellation;

        [ObservableProperty] private List<IStatistiqueData> dailycurrentWeekReturnOrderComplete;

        public int periodeTypeId { get; set; }
        // Propriétés pour chaque graphique
        [ObservableProperty] private Chart weekChart;
        [ObservableProperty] private Chart monthChart;
        [ObservableProperty] private Chart yearChart;

        [ObservableProperty] private Chart weekChartdailycurrentWeekOrderDelivery;
        [ObservableProperty] private Chart weekChartdailyLcurrentWeekOrderCancellation;

        [ObservableProperty] private Chart weekChartdailycurrentWeekReturnOrderComplete;

        [ObservableProperty] bool isWeekChartVisible;

        [ObservableProperty] bool isMonthChartVisible;

        [ObservableProperty] bool isYearChartVisible;
        public List<PeriodeType> PeriodeTypeChoices { get; }
        public OrderCountViewModel(IDialogService? dialogService = null,
            OrderCountUseCase? orderCountUseCase = null)
        {
            _dialogService = dialogService;
            _orderCountUseCase = orderCountUseCase;
            PeriodeTypeChoices = StaticData.StaticData.PeriodeTypeChoices;
        }

        public async Task LoadOrderCountStatisticsAsync()
        {
            if (_orderCountUseCase == null) return;
            try
            {
                var data = await _orderCountUseCase.GetOrderStatistics();
                LoadOderCount(data);
            }
            catch (Exception ex)
            {
                if (_dialogService != null)
                    await _dialogService.ShowErrorAsync(ex);
            }
        }
        public void LoadOderCount(OrderStatistics? data)
        {
            if (data == null)
                return;

            CurrentPeriode = "Semaine en cours";
            LastPeriode = "Semaine précédente";
            TypeDataValue = "Commandes par jour";

            // Utilisation des propriétés de l'interface IAllDataStatistics
            CurrentDataValueWeek = data.CurrentWeekCountAchatClientCompletee;
            LastDataValueWeek = data.LastWeekCountAchatClientCompletee;
            CurrentDataValueMonth = data.CurrentMonthCount;
            LastDataValueMonth = data.LastMonthCount;
            CurrentDataValueYear = data.CurrentYearCount;
            LastDataValueYear = data.LastYearCount;
            CurrentWeekOrderDelivery = data.CurrentWeekCountAchatClientLivree;
            LastWeekOrderDelivery = data.LastWeekCountAchatClientLivree;
            CurrentWeekOrderCancellation = data.CurrentWeekCountAchatClientAnnulation;
            LastWeekOrderCancellation = data.LastWeekCountAchatClientAnnulation;
            CurrentWeekReturnOrderComplete = data.CurrentWeekCountRetourClientCompletee;
            LastWeekReturnOrderComplete = data.LastWeekCountRetourClientCompletee;

            DailyDataValueCurrentWeek = data.DailyCountAchatClientCompleteeForCurrentWeek != null
                ? data.DailyCountAchatClientCompleteeForCurrentWeek.Cast<IStatistiqueData>().ToList()
                : new List<IStatistiqueData>();
            WeeklyDataValueForCurrentMonth = data.WeeklyCountForCurrentMonth != null
                ? data.WeeklyCountForCurrentMonth.Cast<IStatistiqueData>().ToList()
                : new List<IStatistiqueData>();
            MonthlyDataValueForCurrentYear = data.MonthlyCountForCurrentYear != null
                ? data.MonthlyCountForCurrentYear.Cast<IStatistiqueData>().ToList()
                : new List<IStatistiqueData>();
            DailycurrentWeekOrderDelivery = data.DailyCountAchatClientLivreeForCurrentWeek != null
                ? data.DailyCountAchatClientLivreeForCurrentWeek.Cast<IStatistiqueData>().ToList()
                : new List<IStatistiqueData>();
            DailyLcurrentWeekOrderCancellation = data.DailyCountAchatClientAnnulationForCurrentWeek != null
                ? data.DailyCountAchatClientAnnulationForCurrentWeek.Cast<IStatistiqueData>().ToList()
                : new List<IStatistiqueData>();
            DailycurrentWeekReturnOrderComplete = data.DailyCountRetourClientCompleteeForCurrentWeek != null
                ? data.DailyCountRetourClientCompleteeForCurrentWeek.Cast<IStatistiqueData>().ToList()
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
                    TypeDataValue = "Commandes par jour";
                    WeekChart = ChartGenerator.GenerateChart(AllDataValue, isMoney: false);
                    IsWeekChartVisible = true;
                    CurrentWeekOrderDeliveryValue = currentWeekOrderDelivery;
                    LastWeekOrderDeliveryValue = lastWeekOrderDelivery;
                    CurrentWeekOrderCancellationValue = currentWeekOrderCancellation;
                    LastWeekOrderCancellationValue = lastWeekOrderCancellation;
                    CurrentWeekReturnOrderCompleteValue = currentWeekReturnOrderComplete;
                    LastWeekReturnOrderCompleteValue = lastWeekReturnOrderComplete;
                    WeekChartdailycurrentWeekOrderDelivery = ChartGenerator.GenerateChart(dailycurrentWeekOrderDelivery, isMoney: false);
                    WeekChartdailyLcurrentWeekOrderCancellation = ChartGenerator.GenerateChart(dailyLcurrentWeekOrderCancellation, isMoney: false);
                    WeekChartdailycurrentWeekReturnOrderComplete = ChartGenerator.GenerateChart(dailycurrentWeekReturnOrderComplete, isMoney: false);
                    break;

                case 2: // Mois
                    AllDataValue = weeklyDataValueForCurrentMonth;
                    CurrentRevenue = currentDataValueMonth;
                    LastDataValue = lastDataValueMonth;
                    CurrentPeriode = "Mois en cours";
                    LastPeriode = "Mois précédent";
                    TypeDataValue = "Commande mensuelle";
                    MonthChart = ChartGenerator.GenerateChart(AllDataValue, isMoney: false);
                    IsMonthChartVisible = true;
                    break;

                case 3: // Année
                    AllDataValue = monthlyDataValueForCurrentYear;
                    CurrentRevenue = currentDataValueYear;
                    LastDataValue = lastDataValueYear;
                    CurrentPeriode = "Année en cours";
                    LastPeriode = "Année précédente";
                    TypeDataValue = "Commande annuelle";
                    YearChart = ChartGenerator.GenerateChart(AllDataValue, isMoney: false);
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
