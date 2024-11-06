
namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class OrderCountViewModel : BaseViewModel
    {
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
        public OrderCountViewModel()
        {

            PeriodeTypeChoices = StaticData.StaticData.PeriodeTypeChoices;

        }
        public void LoadOderCount(OrderStatistics data)

        {
            CurrentPeriode = "Semaine en cours";
            LastPeriode = "Semaine précédente";
            TypeDataValue = "Commande journalière";

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


            DailyDataValueCurrentWeek = data.DailyCountAchatClientCompleteeForCurrentWeek.Cast<IStatistiqueData>().ToList();
            WeeklyDataValueForCurrentMonth = data.WeeklyCountForCurrentMonth.Cast<IStatistiqueData>().ToList();
            MonthlyDataValueForCurrentYear = data.MonthlyCountForCurrentYear.Cast<IStatistiqueData>().ToList();
            DailycurrentWeekOrderDelivery = data.DailyCountAchatClientLivreeForCurrentWeek.Cast<IStatistiqueData>().ToList();
            DailyLcurrentWeekOrderCancellation = data.DailyCountAchatClientAnnulationForCurrentWeek.Cast<IStatistiqueData>().ToList();
            DailycurrentWeekReturnOrderComplete = data.DailyCountRetourClientCompleteeForCurrentWeek.Cast<IStatistiqueData>().ToList();



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
                    TypeDataValue = "Commande complétée";
                    WeekChart = ChartGenerator.GenerateChart(AllDataValue);
                    IsWeekChartVisible = true;
                    CurrentWeekOrderDeliveryValue = currentWeekOrderDelivery;
                    LastWeekOrderDeliveryValue = lastWeekOrderDelivery;
                    CurrentWeekOrderCancellationValue = currentWeekOrderCancellation;
                    LastWeekOrderCancellationValue = lastWeekOrderCancellation;
                    CurrentWeekReturnOrderCompleteValue = currentWeekReturnOrderComplete;
                    LastWeekReturnOrderCompleteValue = lastWeekReturnOrderComplete;
                    WeekChartdailycurrentWeekOrderDelivery = ChartGenerator.GenerateChart(dailycurrentWeekOrderDelivery);
                    WeekChartdailyLcurrentWeekOrderCancellation = ChartGenerator.GenerateChart(dailyLcurrentWeekOrderCancellation);
                    WeekChartdailycurrentWeekReturnOrderComplete = ChartGenerator.GenerateChart(dailycurrentWeekReturnOrderComplete);
                    break;

                case 2: // Mois
                    AllDataValue = weeklyDataValueForCurrentMonth;
                    CurrentRevenue = currentDataValueMonth;
                    LastDataValue = lastDataValueMonth;
                    CurrentPeriode = "Mois en cours";
                    LastPeriode = "Mois précédent";
                    TypeDataValue = "Commande mensuelle";
                    MonthChart = ChartGenerator.GenerateChart(AllDataValue);
                    IsMonthChartVisible = true;
                    break;

                case 3: // Année
                    AllDataValue = monthlyDataValueForCurrentYear;
                    CurrentRevenue = currentDataValueYear;
                    LastDataValue = lastDataValueYear;
                    CurrentPeriode = "Année en cours";
                    LastPeriode = "Année précédente";
                    TypeDataValue = "Commande annuelle";
                    YearChart = ChartGenerator.GenerateChart(AllDataValue);
                    IsYearChartVisible = true;
                    break;
            }
        }

        [ICommand]
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
