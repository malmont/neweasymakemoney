
using Easymakemoney.Services.Dialog;
using Easymakemoney.UseCase.FraisClientTotalUseCase;

namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class FraisClientTotalViewModel : BaseViewModel
    {
        private readonly IDialogService? _dialogService;
        private readonly GetFraisClientCarrierUseCase? _getFraisClientCarrierUseCase;
        private readonly GetFraisClientTaxUseCase? _getFraisClientTaxUseCase;

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
   
        public FraisClientTotalViewModel(IDialogService? dialogService = null,
            GetFraisClientCarrierUseCase? getFraisClientCarrierUseCase = null,
            GetFraisClientTaxUseCase? getFraisClientTaxUseCase = null)
        {
            _dialogService = dialogService;
            _getFraisClientCarrierUseCase = getFraisClientCarrierUseCase;
            _getFraisClientTaxUseCase = getFraisClientTaxUseCase;
            periodeTypeChoices = new List<PeriodeType>
            {
                new PeriodeType { id = 1, typePeriode = "Taxes", photoPeriode = "week.png" },
                new PeriodeType { id = 2, typePeriode = "Carriers", photoPeriode = "month.png" },
                
            };
        }

        public async Task LoadFraisStatisticsAsync()
        {
            try
            {
                CarrierStatisticsResponse? carrierData = null;
                TaxStatisticsResponse? taxData = null;
                if (_getFraisClientCarrierUseCase != null)
                    carrierData = await _getFraisClientCarrierUseCase.Execute();
                if (_getFraisClientTaxUseCase != null)
                    taxData = await _getFraisClientTaxUseCase.Execute();
                LoadDataFrais(taxData, carrierData);
            }
            catch (Exception ex)
            {
                if (_dialogService != null)
                    await _dialogService.ShowErrorAsync(ex);
            }
        }

        public void LoadDataFrais(TaxStatisticsResponse? data, CarrierStatisticsResponse? data2)
        {
            if (data == null && data2 == null)
                return;

            if (data?.MonthlyTaxesForCurrentYear != null)
            {
                MonhthlyTaxesForCurrentYear = data.MonthlyTaxesForCurrentYear.Cast<IStatistiqueData>().ToList();
                ApplyAlternateRowColors(MonhthlyTaxesForCurrentYear);
            }

            if (data2?.MonthlyCarrierForCurrentYear != null)
            {
                MonthlyCarrierForCurrentYear = data2.MonthlyCarrierForCurrentYear.Cast<IStatistiqueData>().ToList();
                ApplyAlternateRowColors(MonthlyCarrierForCurrentYear);
            }
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

        [RelayCommand]
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
            if (Application.Current?.MainPage != null)
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
