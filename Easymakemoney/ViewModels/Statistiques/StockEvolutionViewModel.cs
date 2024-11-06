
namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class StockEvolutionViewModel : BaseViewModel
    {

        
        [ObservableProperty] private string stockCurrentPeriode;

        [ObservableProperty] private StockValueData stockDataValueCurrentMonth;
      

        // Collections de données pour différentes périodes
        [ObservableProperty] private List<IStatistiqueData> stockMouvementDataValueWeek;
        [ObservableProperty] private List<IStatistiqueData> stockMouvementDataValueMonth;
        [ObservableProperty] private List<IStatistiqueData> stockMouvementDataValueYear;
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

        public StockEvolutionViewModel()
        {

            PeriodeTypeChoices = StaticData.StaticData.PeriodeTypeChoices;

        }
        public void LoadDataChiffreAffaire( StockEvolution data ,StockValue data1)

        {
            StockCurrentPeriode = "stock semaine";
            StockMouvementDataValueWeek = data.StockEvolutionWeek.Cast<IStatistiqueData>().ToList();
            StockMouvementDataValueMonth = data.StockEvolutionMonth.Cast<IStatistiqueData>().ToList();
            StockMouvementDataValueYear = data.StockEvolutionYear.Cast<IStatistiqueData>().ToList();

            StockDataValueCurrentMonth= data1.StockValueCurrent.StockValueCurrentMonth[0];
           

            // Appliquer les couleurs alternées
            ApplyAlternateRowColors(StockMouvementDataValueWeek);
            ApplyAlternateRowColors(StockMouvementDataValueMonth);
            ApplyAlternateRowColors(StockMouvementDataValueYear);
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
                    AllDataValue = stockMouvementDataValueWeek;
                    StockCurrentPeriode = "stock semaine";
                    WeekChart = ChartGenerator.GenerateChart(AllDataValue); 
                    IsWeekChartVisible = true;
                    break;

                case 2: // Mois
                    AllDataValue = stockMouvementDataValueMonth;
                    StockCurrentPeriode = "Stock du mois";
                    MonthChart = ChartGenerator.GenerateChart(AllDataValue); 
                    IsMonthChartVisible = true;
                    break;

                case 3: // Année
                    AllDataValue = stockMouvementDataValueYear;
                    StockCurrentPeriode = "Stock année";
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
