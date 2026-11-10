namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(DocumentId), "DocumentId")]
    public partial class InvoiceReviewPage : ContentPage
    {
        private readonly InvoiceReviewViewModel _viewModel;
        private bool _initialized;

        public InvoiceReviewPage(InvoiceReviewViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public int DocumentId { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Appareil photo et galerie ramènent sur cette page : on n'ouvre la facture qu'une fois.
            if (_initialized)
                return;

            _initialized = true;
            await _viewModel.InitAsync(DocumentId);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // La dernière correction part tout de suite, sans attendre la seconde de calme.
            _viewModel.Stop();
        }
    }
}
