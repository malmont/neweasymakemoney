

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

        private readonly StatistiquesPageLoader _loader = new();

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _loader.RunOnceAsync(async () =>
            {
                var data = await _averageOrderValueUseCase.GetAverageOrderValueStatistics();
                _viewModel.LoadDataChiffreAffaire(data);
                _viewModel.UpdateRevenueData(1);
            });
        }
    }
}