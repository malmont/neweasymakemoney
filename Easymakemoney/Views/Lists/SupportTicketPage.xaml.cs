namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(TicketId), "TicketId")]
    public partial class SupportTicketPage : ContentPage
    {
        private readonly SupportTicketViewModel _viewModel;

        public SupportTicketPage(SupportTicketViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public string TicketId
        {
            set => _viewModel.TicketId = int.TryParse(value, out var id) ? id : 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadAsync();
        }
    }
}
