
namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class PaiementRemboursementViewModel : BaseViewModel
    {

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
   
        public PaiementRemboursementViewModel()
        {
            periodeTypeChoices = new List<PeriodeType>
            {
                new PeriodeType { id = 1, typePeriode = "Paiements", photoPeriode = "periode_semaine.png" },
                new PeriodeType { id = 2, typePeriode = "Remboursement", photoPeriode = "periode_mois.png" },
                
            };
        }

        public void LoadDataPaiement(PaiementRemboursementEvolution data)
        {
            // Totaux
            PaiementTotalSemaine = data.CurrentWeek.PaiementClient.Total;
            RemboursementTotalSemaine = data.CurrentWeek.RemboursementClient.Total;

            PaiementTotalMois = data.CurrentMonth.PaiementClient;
            RemboursementTotalMois = data.CurrentMonth.RemboursementClient;

            PaiementTotalAnnee = data.CurrentYear.PaiementClient;
            RemboursementTotalAnnee = data.CurrentYear.RemboursementClient;

            // Initialisation des collections
            PaiementQuotidienSemaine = data.CurrentWeek.PaiementClient.Daily.Cast<IStatistiqueData>().ToList();
            RemboursementQuotidienSemaine = data.CurrentWeek.RemboursementClient.Daily.Cast<IStatistiqueData>().ToList();

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
                    Transaction = paiementQuotidienSemaine;
                    PaymentChart = ChartGenerator.GenerateChart(transaction); 
                    IsPaiementChartVisible = true;
                    break;

                case 2:
                    TransactionLabel = "Remboursement";
                    Transaction = RemboursementQuotidienSemaine;
                    RefundChart = ChartGenerator.GenerateChart(transaction); 
                    IsRemboursementChartVisible = true;
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
                        UpdatePaiementData(periodeTypeId);
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
