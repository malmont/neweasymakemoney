namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(CommandId), "CommandId")]
    public partial class AttachProductsPage : ContentPage
    {
        private readonly AttachProductsViewModel _viewModel;

        public AttachProductsPage(AttachProductsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public int CommandId { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.CommandId = CommandId;
            await _viewModel.LoadFirstPageAsync();
        }
    }
}
