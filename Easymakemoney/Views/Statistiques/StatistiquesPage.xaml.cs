
namespace Easymakemoney.Views.Statistiques
{
    [QueryProperty(nameof(Range), "Range")]
    public partial class StatistiquesPage : ContentPage
    {
        private readonly StatistiquesViewModel _viewModel;

        public StatistiquesPage(StatistiquesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public string Range
        {
            set => _viewModel.UseInitialRange(value);
        }

        /// <summary>Chargé une fois : le retour d'un écran détaillé ou la fermeture d'un sélecteur garde la plage choisie.</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }
    }
}
