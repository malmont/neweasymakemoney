namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(PeriodId), "PeriodId")]
    [QueryProperty(nameof(ReadOnly), "ReadOnly")]
    public partial class PeriodBudgetPage : ContentPage
    {
        private readonly PeriodBudgetViewModel _viewModel;

        public PeriodBudgetPage(PeriodBudgetViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public string PeriodId
        {
            set => _viewModel.PeriodId = int.TryParse(value, out var id) ? id : 0;
        }

        public string ReadOnly
        {
            set => _viewModel.ReadOnly = value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadAsync();
        }
    }
}
