namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(CommandId), "CommandId")]
    [QueryProperty(nameof(Closed), "Closed")]
    public partial class ListNewProductPage : ContentPage
    {
        private readonly ListNewProductViewModel _viewModel;

        public ListNewProductPage(ListNewProductViewModel viewModel)
        {
            InitializeComponent(); // Ceci est auto-généré si tout est correct
            BindingContext = _viewModel = viewModel;
        }

        public int CommandId { get; set; }

        public bool Closed { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.CommandId = CommandId;
            _viewModel.IsClosed = Closed;
            await _viewModel.GetListProductAsync();
        }
    }
}
