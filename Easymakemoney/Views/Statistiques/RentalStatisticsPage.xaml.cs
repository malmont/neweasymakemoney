namespace Easymakemoney.Views.Statistiques
{
    public partial class RentalStatisticsPage : ContentPage
    {
        private readonly RentalStatisticsViewModel _viewModel;

        public RentalStatisticsPage(RentalStatisticsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        /// <summary>Chargé une fois : un sélecteur qui se ferme ne remet pas la plage à zéro.</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }
    }
}
