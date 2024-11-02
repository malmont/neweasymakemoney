

namespace Easymakemoney.Views.Statistiques

{
    public partial class PaiementRemboursementPage: ContentPage
    {
        private readonly  PaiementRemboursementUseCase _paiementRemboursementUseCase;
         private readonly PaiementRemboursementViewModel _viewModel;
        public PaiementRemboursementPage(PaiementRemboursementViewModel viewModel, PaiementRemboursementUseCase paiementRemboursementUseCase)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.BindingContext = _viewModel;
            _paiementRemboursementUseCase = paiementRemboursementUseCase;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var data = await _paiementRemboursementUseCase.GetPaiementRemboursementEvolutionAsync();
            _viewModel.LoadDataPaiement(data);
            _viewModel.UpdatePaiementData(1);
        
        }
    }
}