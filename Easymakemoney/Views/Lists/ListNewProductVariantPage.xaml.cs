namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(ProductId), "ProductId")]
    [QueryProperty(nameof(Transfer), "Transfer")]
    public partial class ListNewProductVariantPage : ContentPage
    {
         private readonly ListNewProductsVariantViewModel _viewModel;

        public ListNewProductVariantPage(ListNewProductsVariantViewModel viewModel)
        {
            InitializeComponent(); // Ceci est auto-généré si tout est correct
            BindingContext = _viewModel = viewModel;
        }

         public int ProductId { get; set; }

        /// <summary>Article d'une commande d'arrivée d'un transfert du groupe (G-5).</summary>
        public bool Transfer { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.ProductId = ProductId;
            _viewModel.IsGroupTransfer = Transfer;
            await _viewModel.GetlistProductVariantAsync();
        }


    }

        
}