namespace Easymakemoney.Views.Lists
{
    public partial class TreasuryPage : ContentPage
    {
        private readonly TreasuryViewModel _viewModel;

        public TreasuryPage(TreasuryViewModel viewModel)
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
