namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(ProductId), "ProductId")]
    public partial class ListNewProductVariantPage : ContentPage
    {
         private readonly ListNewProductsVariantViewModel _viewModel;

        public ListNewProductVariantPage(ListNewProductsVariantViewModel viewModel)
        {
            InitializeComponent(); // Ceci est auto-généré si tout est correct
            BindingContext = _viewModel = viewModel;
        }

         public int ProductId { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.ProductId = ProductId;
            await _viewModel.GetlistProductVariantAsync();
        }


    }

        
}