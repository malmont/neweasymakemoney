namespace Easymakemoney.Views.Lists
{
    public partial class FixedCostsPage : ContentPage
    {
        private readonly FixedCostsViewModel _viewModel;

        public FixedCostsPage(FixedCostsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadAsync();
        }
    }
}
