namespace Easymakemoney.Views.Statistiques
{
    public partial class RentalGridPage : ContentPage
    {
        private readonly RentalGridViewModel _viewModel;

        public RentalGridPage(RentalGridViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        /// <summary>Chargé une fois : une fenêtre qui se ferme ne remet pas la saisie à zéro.</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }
    }
}
