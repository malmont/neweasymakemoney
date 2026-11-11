namespace Easymakemoney.Views.Lists
{
    public partial class FiscalYearPage : ContentPage
    {
        private readonly FiscalYearViewModel _viewModel;

        public FiscalYearPage(FiscalYearViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }
    }
}
