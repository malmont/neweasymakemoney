namespace Easymakemoney.Views.Lists
{
    public partial class VariantPromotionListPage : ContentPage
    {
        private readonly VariantPromotionListViewModel _viewModel;

        public VariantPromotionListPage(VariantPromotionListViewModel viewModel)
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
