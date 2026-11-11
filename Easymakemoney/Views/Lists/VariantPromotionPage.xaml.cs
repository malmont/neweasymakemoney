namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(ProductId), "ProductId")]
    [QueryProperty(nameof(ProductName), "ProductName")]
    public partial class VariantPromotionPage : ContentPage
    {
        private readonly VariantPromotionViewModel _viewModel;
        private int _productId;
        private string? _productName;
        private bool _initialized;

        public VariantPromotionPage(VariantPromotionViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public string ProductId { set => _productId = int.TryParse(value, out var id) ? id : 0; }
        public string ProductName { set => _productName = value; }

        /// <summary>Initialisé une fois : un sélecteur de date qui se ferme ne remet pas le formulaire à zéro.</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_initialized)
                return;
            _initialized = true;
            _viewModel.Init(_productId, _productName);
            await _viewModel.ComputePreviewCommand.ExecuteAsync(null);
        }
    }
}
