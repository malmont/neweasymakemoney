namespace Easymakemoney.Views.Lists
{
    public partial class SupportTicketListPage : ContentPage
    {
        private readonly SupportTicketListViewModel _viewModel;

        public SupportTicketListPage(SupportTicketListViewModel viewModel)
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
