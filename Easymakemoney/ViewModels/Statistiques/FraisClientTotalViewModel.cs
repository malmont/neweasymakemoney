
namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class FraisClientTotalViewModel : BaseViewModel
    {

        [ObservableProperty] private string titreFraisMonth;
  
        [ObservableProperty] private List<IStatistiqueData> monhthlyTaxesForCurrentYear;
        [ObservableProperty] private List<IStatistiqueData> monthlyCarrierForCurrentYear;

        [ObservableProperty] private List<IStatistiqueData> fraisTotal;


        public int periodeTypeId { get; set; }
        public List<PeriodeType> periodeTypeChoices { get; set; }

        // Propriétés pour chaque graphique
        [ObservableProperty] private Chart monthlyCarrierChart;
        [ObservableProperty] private Chart monhthlyTaxesChart;
   

        [ObservableProperty] bool ismonthlyCarrierChartVisible;
        [ObservableProperty] bool ismonhthlyTaxesChartVisible;
   
        public FraisClientTotalViewModel()
        {
            periodeTypeChoices = new List<PeriodeType>
            {
                new PeriodeType { id = 1, typePeriode = "Taxes", photoPeriode = "week.png" },
                new PeriodeType { id = 2, typePeriode = "Carriers", photoPeriode = "month.png" },
                
            };
        }

        public void LoadDataFrais( TaxStatisticsResponse data , CarrierStatisticsResponse data2)
        {
            MonhthlyTaxesForCurrentYear = data.MonthlyTaxesForCurrentYear.Cast<IStatistiqueData>().ToList();
            MonthlyCarrierForCurrentYear = data2.MonthlyCarrierForCurrentYear.Cast<IStatistiqueData>().ToList();


            
            ApplyAlternateRowColors(MonhthlyTaxesForCurrentYear);
            ApplyAlternateRowColors(MonthlyCarrierForCurrentYear);
        }

        public void UpdateFraisData(int periodeTypeId)
        {
            IsmonhthlyTaxesChartVisible = false;
            IsmonthlyCarrierChartVisible = false;
  

            switch (periodeTypeId)
            {
                case 1:
                    TitreFraisMonth = "Taxe du mois";
                    FraisTotal = monhthlyTaxesForCurrentYear;
                    MonhthlyTaxesChart = ChartGenerator.GenerateChart(MonhthlyTaxesForCurrentYear); 
                    IsmonhthlyTaxesChartVisible = true;
                    break;

                case 2:
                    TitreFraisMonth = "Carrier du mois";
                    FraisTotal = monthlyCarrierForCurrentYear;
                    MonthlyCarrierChart = ChartGenerator.GenerateChart(fraisTotal); 
                    IsmonthlyCarrierChartVisible = true;
                    break;

              
            }
        }

        [ICommand]
        public async Task ShowBottomSheetPopupListView()
        {
            var viewModel = new BottomSheetPopupListViewViewModel(
                getItemsFunc: async () => await Task.FromResult(periodeTypeChoices),
                onItemTappedAction: async (item) =>
                {
                    if (item is PeriodeType periodeType)
                    {
                        periodeTypeId = periodeType.id;
                        UpdateFraisData(periodeTypeId);
                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            await Application.Current.MainPage.ShowPopupAsync(popup);
        }

        private void ApplyAlternateRowColors<T>(List<T> dataList) where T : class
        {
            for (int i = 0; i < dataList.Count; i++)
            {
                if (dataList[i] is IStatistiqueData statData)
                {
                    statData.BackgroundColor = i % 2 == 0
                        ? Colors.LightGray.WithAlpha(0.3f)
                        : Colors.Transparent;
                }
            }
        }
    }
}
