namespace Easymakemoney.Views.Dasboard
{
    public partial class DashBoardCollectionPage : ContentPage
    {
        public DashBoardCollectionPage(DashBoardCollectionViewModel viewModel)
        {
            InitializeComponent();
            this.BindingContext = _viewModel = viewModel;
        }

        private readonly DashBoardCollectionViewModel _viewModel;

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.SelectCurrentPeriodIfNoneAsync();
        }
    }
}
