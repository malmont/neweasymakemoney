namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(PeriodId), "PeriodId")]
    public partial class PeriodSimulationPage : ContentPage
    {
        private readonly PeriodSimulationViewModel _viewModel;

        public PeriodSimulationPage(PeriodSimulationViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public string PeriodId
        {
            set => _viewModel.PeriodId = int.TryParse(value, out var id) ? id : 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.StartAsync();
        }
    }
}
