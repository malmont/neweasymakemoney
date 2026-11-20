namespace Easymakemoney.Views.Lists
{
    public partial class LoanListPage : ContentPage
    {
        private readonly LoanListViewModel _viewModel;

        public LoanListPage(LoanListViewModel viewModel)
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
