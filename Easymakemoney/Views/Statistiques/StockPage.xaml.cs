

namespace Easymakemoney.Views.Statistiques
{
    public partial class StockPage : ContentPage
    {
        private readonly StockEvolutionViewModel _viewModel;
        private readonly StockEvolutionUseCase _stockEvolutionUseCase;

        private readonly StockValueEvolutionUseCase _stockValueEvolutionUseCase;

        
        public StockPage(StockEvolutionViewModel viewModel, StockEvolutionUseCase stockEvolutionUseCase, StockValueEvolutionUseCase stockValueEvolutionUseCase)
        {
            InitializeComponent();
            _stockEvolutionUseCase = stockEvolutionUseCase;
            _stockValueEvolutionUseCase = stockValueEvolutionUseCase;
            _viewModel = viewModel;
            this.BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var data = await _stockEvolutionUseCase.GetStockEvolution();
            var dataValue = await _stockValueEvolutionUseCase.GetStockValueEvolution();
            _viewModel.LoadDataChiffreAffaire(data,dataValue);
            _viewModel.UpdateRevenueData(1);
        }
    }
}