
namespace Easymakemoney.Views.Statistiques
{
    public partial class StatistiquesPage : ContentPage
    {
        private readonly StatistiquesViewModel _viewModel;

        public StatistiquesPage(StatistiquesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        /// <summary>Chargé une fois : le retour d'un écran détaillé ou la fermeture d'un sélecteur garde la plage choisie.</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }
    }
}
