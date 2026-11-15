namespace Easymakemoney.Views.Lists
{
    public partial class BarcodePage : ContentPage
    {
        private readonly BarcodeViewModel _viewModel;

        public BarcodePage(BarcodeViewModel viewModel)
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
