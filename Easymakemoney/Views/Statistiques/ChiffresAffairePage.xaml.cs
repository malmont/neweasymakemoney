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

        private readonly StatistiquesPageLoader _loader = new();

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _loader.RunOnceAsync(async () =>
            {
                var data = await _chiffreAffaireUseCase.GetRevenueStatistics();
                _viewModel.LoadDataChiffreAffaire(data);
                _viewModel.UpdateRevenueData(1);
            });
        }
    }
}
