namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(CommandId), "CommandId")]
    [QueryProperty(nameof(Closed), "Closed")]
    [QueryProperty(nameof(Transfer), "Transfer")]
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

        /// <summary>Commande d'arrivée d'un transfert du groupe (G-5).</summary>
        public bool Transfer { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.CommandId = CommandId;
            _viewModel.IsClosed = Closed;
            _viewModel.IsGroupTransfer = Transfer;
            await _viewModel.GetListProductAsync();
        }
    }
}
