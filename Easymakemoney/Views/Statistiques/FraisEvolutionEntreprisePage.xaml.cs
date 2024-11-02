using Easymakemoney.UseCase.FraisEvolutionUseCase;

namespace Easymakemoney.Views.Statistiques
{
    public partial class FraisEvolutionEntreprisePage : ContentPage
    {
        private readonly FraisEvolutionUseCase _fraisEvolutionUseCase;
        private readonly FraisEvolutionEntrepriseViewModel _viewModel;

        public FraisEvolutionEntreprisePage(FraisEvolutionUseCase fraisEvolutionUseCase, FraisEvolutionEntrepriseViewModel viewModel)
        {
            InitializeComponent();
            _fraisEvolutionUseCase = fraisEvolutionUseCase;
            _viewModel = viewModel;

            this.BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var data = await _fraisEvolutionUseCase.GetFraisEvolutionAsync();
            _viewModel.LoadDataFrais(data);
            _viewModel.UpdateFraisData(1);
        }
    }
}
