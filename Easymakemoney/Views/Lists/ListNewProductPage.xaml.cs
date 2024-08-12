namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(CommandId), "CommandId")]
    public partial class ListNewProductPage : ContentPage
    {
        private readonly ListNewProductViewModel _viewModel;

        public ListNewProductPage(ListNewProductViewModel viewModel)
        {
            InitializeComponent(); // Ceci est auto-généré si tout est correct
            BindingContext = _viewModel = viewModel;
        }

        public int CommandId { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.CommandId = CommandId;
            await _viewModel.GetListProductAsync();
        }
    }
}
