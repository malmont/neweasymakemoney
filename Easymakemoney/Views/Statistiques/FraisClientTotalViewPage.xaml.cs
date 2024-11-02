

using Easymakemoney.UseCase.FraisClientTotalUseCase;

namespace Easymakemoney.Views.Statistiques

{
    public partial class FraisClientTotalViewPage: ContentPage
    {
        private readonly FraisClientTotalViewModel _viewModel;
        private readonly GetFraisClientCarrierUseCase _getFraisClientCarrierUseCase;
        private readonly GetFraisClientTaxUseCase _getFraisClientTaxUseCase;
        public FraisClientTotalViewPage(FraisClientTotalViewModel viewModel, GetFraisClientCarrierUseCase getFraisClientCarrierUseCase, GetFraisClientTaxUseCase getFraisClientTaxUseCase)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _getFraisClientCarrierUseCase = getFraisClientCarrierUseCase;
            _getFraisClientTaxUseCase = getFraisClientTaxUseCase;
            this.BindingContext = _viewModel;
        }
      

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var CarrierData = await _getFraisClientCarrierUseCase.Execute();
            var TaxData = await _getFraisClientTaxUseCase.Execute();
            _viewModel.LoadDataFrais(TaxData, CarrierData);
             _viewModel.UpdateFraisData(1);
        }
    }
}