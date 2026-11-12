// Code antérieur aux annotations de nullité : avertissements coupés ici, sans effet sur l'exécution.
#nullable disable warnings

using Easymakemoney.Services.Dialog;
using Easymakemoney.UseCase.StockEvolutionUseCase;

namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class StockEvolutionViewModel : BaseViewModel
    {
        private readonly IDialogService? _dialogService;
        private readonly StockEvolutionUseCase? _stockEvolutionUseCase;
        private readonly StockValueEvolutionUseCase? _stockValueEvolutionUseCase;

        
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

        public StockEvolutionViewModel(IDialogService? dialogService = null,
            StockEvolutionUseCase? stockEvolutionUseCase = null,
            StockValueEvolutionUseCase? stockValueEvolutionUseCase = null)
        {
            _dialogService = dialogService;
            _stockEvolutionUseCase = stockEvolutionUseCase;
            _stockValueEvolutionUseCase = stockValueEvolutionUseCase;
            PeriodeTypeChoices = StaticData.StaticData.PeriodeTypeChoices;
        }

        public async Task LoadStockEvolutionStatisticsAsync()
        {
            try
            {
                StockEvolution? stockData = null;
                StockValue? stockValueData = null;
                if (_stockEvolutionUseCase != null)
                    stockData = await _stockEvolutionUseCase.GetStockEvolution();
                if (_stockValueEvolutionUseCase != null)
                    stockValueData = await _stockValueEvolutionUseCase.GetStockValueEvolution();
                LoadDataChiffreAffaire(stockData, stockValueData);
            }
            catch (Exception ex)
            {
                if (_dialogService != null)
                    await _dialogService.ShowErrorAsync(ex);
            }
        }
        public void LoadDataChiffreAffaire(StockEvolution? data, StockValue? data1)
        {
            if (data == null && data1 == null)
                return;

            StockCurrentPeriode = "stock semaine";

            if (data != null)
            {
                StockMouvementDataValueWeek = data.StockEvolutionWeek != null
                    ? data.StockEvolutionWeek.Cast<IStatistiqueData>().ToList()
                    : new List<IStatistiqueData>();
                StockMouvementDataValueMonth = data.StockEvolutionMonth != null
                    ? data.StockEvolutionMonth.Cast<IStatistiqueData>().ToList()
                    : new List<IStatistiqueData>();
                StockMouvementDataValueYear = data.StockEvolutionYear != null
                    ? data.StockEvolutionYear.Cast<IStatistiqueData>().ToList()
                    : new List<IStatistiqueData>();

                ApplyAlternateRowColors(StockMouvementDataValueWeek);
                ApplyAlternateRowColors(StockMouvementDataValueMonth);
                ApplyAlternateRowColors(StockMouvementDataValueYear);
            }

            if (data1?.StockValueCurrent?.StockValueCurrentMonth != null && data1.StockValueCurrent.StockValueCurrentMonth.Count > 0)
            {
                StockDataValueCurrentMonth = data1.StockValueCurrent.StockValueCurrentMonth[0];
            }
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
                    AllDataValue = StockMouvementDataValueWeek;
                    StockCurrentPeriode = "stock semaine";
                    WeekChart = ChartGenerator.GenerateChart(AllDataValue, isMoney: false); 
                    IsWeekChartVisible = true;
                    break;

                case 2: // Mois
                    AllDataValue = StockMouvementDataValueMonth;
                    StockCurrentPeriode = "Stock du mois";
                    MonthChart = ChartGenerator.GenerateChart(AllDataValue, isMoney: false); 
                    IsMonthChartVisible = true;
                    break;

                case 3: // Année
                    AllDataValue = StockMouvementDataValueYear;
                    StockCurrentPeriode = "Stock année";
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
            if (Application.Current?.Windows.FirstOrDefault()?.Page is { } page)
                await page.ShowPopupAsync(popup);
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
