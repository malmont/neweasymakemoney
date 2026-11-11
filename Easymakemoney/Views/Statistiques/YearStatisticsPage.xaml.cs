namespace Easymakemoney.Views.Statistiques
{
    public partial class YearStatisticsPage : ContentPage
    {
        private readonly YearStatisticsViewModel _viewModel;

        public YearStatisticsPage(YearStatisticsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        /// <summary>Chargé une fois ; rechargé au retour seulement si le mois de début de l'exercice a changé.</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }
    }
}
