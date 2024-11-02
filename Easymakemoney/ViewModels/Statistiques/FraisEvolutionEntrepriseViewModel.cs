
using static Easymakemoney.Models.FraisEvolution;

namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class FraisEvolutionEntrepriseViewModel : BaseViewModel
    {
        // Propriétés pour les totaux des frais
        [ObservableProperty] private double totalFraisAnnee;
        [ObservableProperty] private double noteDeFraisAnnee;
        [ObservableProperty] private double fraisDePortAnnee;

        [ObservableProperty] private string periodeFrais;

        [ObservableProperty] private string labelFrais;

        // Collections pour les frais par mois et par jour
        [ObservableProperty] private List<MonthlyFraisData> fraisParMois;
        [ObservableProperty] private List<DailyFraisData> fraisParJour;

        [ObservableProperty] private List<IFraisData>? allFraisValue;
        public int periodeTypeId { get; set; }
        public List<PeriodeType> periodeTypeChoices { get; set; }

        // Propriétés pour chaque graphique
        [ObservableProperty] private Chart monthChart;
        [ObservableProperty] private Chart yearChart;

        [ObservableProperty] bool isMonthChartVisible;
        [ObservableProperty] bool isYearChartVisible;

        public FraisEvolutionEntrepriseViewModel()
        {
            periodeTypeChoices = new List<PeriodeType>
            {
                new PeriodeType { id = 1, typePeriode = "Mois", photoPeriode = "periode_mois.png" },
                new PeriodeType { id = 2, typePeriode = "Année", photoPeriode = "periode_annee.png" }
            };
        }

        public void LoadDataFrais(FraisEvolution data)
        {

            TotalFraisAnnee = data.TotalFraisAnnee.TotalFrais.Total;
            NoteDeFraisAnnee = data.TotalFraisAnnee.TotalFrais.NoteDeFrais;
            FraisDePortAnnee = data.TotalFraisAnnee.TotalFrais.FraisDePort;

            LabelFrais = "frais par mois";
            FraisParMois = data.FraisParMois;
            FraisParJour = data.FraisParJour;
            PeriodeFrais = "Frais annuels";

            ApplyAlternateRowColors(fraisParMois);
            ApplyAlternateRowColors(fraisParJour);
        }

        public void UpdateFraisData(int periodeTypeId)
        {
            IsMonthChartVisible = false;
            IsYearChartVisible = false;

            switch (periodeTypeId)
            {
                case 1:
                    PeriodeFrais = "Frais annuels";
                    AllFraisValue = fraisParMois.Cast<IFraisData>().ToList();
                    LabelFrais = "frais par mois";
                    IsMonthChartVisible = true;
                    break;
                case 2:
                    PeriodeFrais = "Frais quotidien";
                    AllFraisValue = fraisParJour.Cast<IFraisData>().ToList();
                    LabelFrais = "frais par Jours";
                    IsYearChartVisible = true;
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
