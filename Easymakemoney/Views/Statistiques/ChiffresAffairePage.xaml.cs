namespace Easymakemoney.Views.Statistiques
{
    public partial class ChiffresAffairePage : ContentPage
    {
        private readonly ChiffreAffaireUseCase _chiffreAffaireUseCase;
        private readonly StatistiquesDataValueViewmodel _viewModel;

        public ChiffresAffairePage(ChiffreAffaireUseCase chiffreAffaireUseCase, StatistiquesDataValueViewmodel viewModel)
        {
            InitializeComponent();
            _chiffreAffaireUseCase = chiffreAffaireUseCase;
            _viewModel = viewModel;

            this.BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var data = await _chiffreAffaireUseCase.GetRevenueStatistics();
            _viewModel.LoadDataChiffreAffaire(data);
            _viewModel.UpdateRevenueData(1);
        }
    }
}
