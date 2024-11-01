

namespace Easymakemoney.Views.Statistiques

{
    public partial class PanierMoyenPage: ContentPage
    {
        private readonly StatistiquesDataValueViewmodel _viewModel;
        private readonly AverageOrderValueUseCase _averageOrderValueUseCase;
        public PanierMoyenPage(StatistiquesDataValueViewmodel viewModel, AverageOrderValueUseCase averageOrderValueUseCase)
        {
            InitializeComponent();
            _averageOrderValueUseCase = averageOrderValueUseCase;
            _viewModel = viewModel;
             this.BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var data = await _averageOrderValueUseCase.GetAverageOrderValueStatistics();
            _viewModel.LoadDataChiffreAffaire(data);
            _viewModel.UpdateRevenueData(1);
        }
    }
}