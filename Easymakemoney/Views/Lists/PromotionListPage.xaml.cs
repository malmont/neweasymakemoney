namespace Easymakemoney.Views.Lists
{
    public partial class PromotionListPage : ContentPage
    {
        private readonly PromotionListViewModel _viewModel;

        public PromotionListPage(PromotionListViewModel viewModel)
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
