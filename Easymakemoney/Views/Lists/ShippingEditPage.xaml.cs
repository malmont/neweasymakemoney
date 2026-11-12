namespace Easymakemoney.Views.Lists
{
    public partial class ShippingEditPage : ContentPage
    {
        private readonly ShippingEditViewModel _viewModel;

        public ShippingEditPage(ShippingEditViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadTransporteursAsync();
        }
    }
}
