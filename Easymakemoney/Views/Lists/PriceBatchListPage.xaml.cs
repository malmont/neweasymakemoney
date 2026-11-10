namespace Easymakemoney.Views.Lists
{
    public partial class PriceBatchListPage : ContentPage
    {
        private readonly PriceBatchListViewModel _viewModel;

        public PriceBatchListPage(PriceBatchListViewModel viewModel)
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
