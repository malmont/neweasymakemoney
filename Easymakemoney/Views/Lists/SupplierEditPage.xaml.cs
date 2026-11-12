namespace Easymakemoney.Views.Lists
{
    public partial class SupplierEditPage : ContentPage
    {
        private readonly SupplierEditViewModel _viewModel;

        public SupplierEditPage(SupplierEditViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadTypesAsync();
        }
    }
}
