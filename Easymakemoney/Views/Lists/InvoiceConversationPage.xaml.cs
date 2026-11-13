namespace Easymakemoney.Views.Lists
{
    /// <summary>Lot IA-FACT-1 : la conversation d'une facture fournisseur, de la lecture à la commande.</summary>
    [QueryProperty(nameof(DocumentId), "DocumentId")]
    public partial class InvoiceConversationPage : ContentPage
    {
        private readonly InvoiceConversationViewModel _viewModel;
        private int _documentId;

        public InvoiceConversationPage(InvoiceConversationViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public string DocumentId
        {
            set => _documentId = int.TryParse(value, out var id) ? id : 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_documentId <= 0)
                return;
            if (_viewModel.Conversation == null)
                await _viewModel.InitAsync(_documentId);
            else
                await _viewModel.ReloadAsync(); // retour de « Vérifier en détail » : le serveur a pu changer le fil
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.Stop();
        }
    }
}
