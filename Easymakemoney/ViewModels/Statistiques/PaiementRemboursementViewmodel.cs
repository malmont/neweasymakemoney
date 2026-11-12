// Code antérieur aux annotations de nullité : avertissements coupés ici, sans effet sur l'exécution.
#nullable disable warnings

using Easymakemoney.Services.Dialog;
using Easymakemoney.UseCase.PaiementRemboursementUseCase;

namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class PaiementRemboursementViewModel : BaseViewModel
    {
        private readonly IDialogService? _dialogService;
        private readonly PaiementRemboursementUseCase? _paiementRemboursementUseCase;

        [ObservableProperty] private double paiementTotalSemaine;
        [ObservableProperty] private double remboursementTotalSemaine;

        [ObservableProperty] private double paiementTotalMois;
        [ObservableProperty] private double remboursementTotalMois;

        [ObservableProperty] private double paiementTotalAnnee;
        [ObservableProperty] private double remboursementTotalAnnee;

        [ObservableProperty] private string transactionLabel;
       


        [ObservableProperty] private List<IStatistiqueData> paiementQuotidienSemaine;
        [ObservableProperty] private List<IStatistiqueData> remboursementQuotidienSemaine;

        [ObservableProperty] private List<IStatistiqueData> transaction;


        public int periodeTypeId { get; set; }
        public List<PeriodeType> periodeTypeChoices { get; set; }

        // Propriétés pour chaque graphique
        [ObservableProperty] private Chart paymentChart;
        [ObservableProperty] private Chart refundChart;
   

        [ObservableProperty] bool isPaiementChartVisible;
        [ObservableProperty] bool isRemboursementChartVisible;
   
        public PaiementRemboursementViewModel(IDialogService? dialogService = null,
            PaiementRemboursementUseCase? paiementRemboursementUseCase = null)
        {
            _dialogService = dialogService;
            _paiementRemboursementUseCase = paiementRemboursementUseCase;
            periodeTypeChoices = new List<PeriodeType>
            {
                new PeriodeType { id = 1, typePeriode = "Paiements", photoPeriode = "week.png" },
                new PeriodeType { id = 2, typePeriode = "Remboursement", photoPeriode = "month.png" },
                
            };
        }

        public async Task LoadPaiementRemboursementStatisticsAsync()
        {
            if (_paiementRemboursementUseCase == null) return;
            try
            {
                var data = await _paiementRemboursementUseCase.GetPaiementRemboursementEvolutionAsync();
                LoadDataPaiement(data);
            }
            catch (Exception ex)
            {
                if (_dialogService != null)
                    await _dialogService.ShowErrorAsync(ex);
            }
        }

        public void LoadDataPaiement(PaiementRemboursementEvolution? data)
        {
            if (data == null)
                return;

            // Totaux
            if (data.CurrentWeek?.PaiementClient != null)
                PaiementTotalSemaine = data.CurrentWeek.PaiementClient.Total;
            if (data.CurrentWeek?.RemboursementClient != null)
                RemboursementTotalSemaine = data.CurrentWeek.RemboursementClient.Total;

            if (data.CurrentMonth != null)
            {
                PaiementTotalMois = data.CurrentMonth.PaiementClient;
                RemboursementTotalMois = data.CurrentMonth.RemboursementClient;
            }

            if (data.CurrentYear != null)
            {
                PaiementTotalAnnee = data.CurrentYear.PaiementClient;
                RemboursementTotalAnnee = data.CurrentYear.RemboursementClient;
            }

            // Initialisation des collections
            PaiementQuotidienSemaine = data.CurrentWeek?.PaiementClient?.Daily != null
                ? data.CurrentWeek.PaiementClient.Daily.Cast<IStatistiqueData>().ToList()
                : new List<IStatistiqueData>();
            RemboursementQuotidienSemaine = data.CurrentWeek?.RemboursementClient?.Daily != null
                ? data.CurrentWeek.RemboursementClient.Daily.Cast<IStatistiqueData>().ToList()
                : new List<IStatistiqueData>();

            TransactionLabel = "Paiements";
            
            ApplyAlternateRowColors(PaiementQuotidienSemaine);
            ApplyAlternateRowColors(RemboursementQuotidienSemaine);
        }

        public void UpdatePaiementData(int periodeTypeId)
        {
            IsPaiementChartVisible = false;
            IsRemboursementChartVisible = false;
  

            switch (periodeTypeId)
            {
                case 1:
                    TransactionLabel = "Paiements";
                    Transaction = PaiementQuotidienSemaine;
                    PaymentChart = ChartGenerator.GenerateChart(Transaction); 
                    IsPaiementChartVisible = true;
                    break;

                case 2:
                    TransactionLabel = "Remboursement";
                    Transaction = RemboursementQuotidienSemaine;
                    RefundChart = ChartGenerator.GenerateChart(Transaction); 
                    IsRemboursementChartVisible = true;
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
                        UpdatePaiementData(periodeTypeId);
                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            if (Application.Current?.Windows.FirstOrDefault()?.Page is { } page)
                await page.ShowPopupAsync(popup);
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
