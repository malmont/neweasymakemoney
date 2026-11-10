namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(CampaignId), "CampaignId")]
    [QueryProperty(nameof(DocumentId), "DocumentId")]
    [QueryProperty(nameof(FromCampaignId), "FromCampaignId")]
    public partial class CampaignFormPage : ContentPage
    {
        private readonly CampaignFormViewModel _viewModel;
        private bool _initialized;
        private int _documentId;

        public CampaignFormPage(CampaignFormViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public int CampaignId { get; set; }

        /// <summary>Relance d'une campagne rentable (conseil du guide) : nouvelle campagne préremplie.</summary>
        public int FromCampaignId { get; set; }

        /// <summary>
        /// Facture de régie lue : DocumentReadPage revient ici avec « ..?DocumentId= » ; le formulaire déjà ouvert
        /// se préremplit alors sans perdre la saisie.
        /// </summary>
        public int DocumentId
        {
            get => _documentId;
            set
            {
                _documentId = value;
                if (_initialized && value > 0)
                    _ = _viewModel.UseDocumentAsync(value);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Retour de la lecture de facture ou d'un sélecteur : on n'initialise qu'une fois.
            if (_initialized)
                return;

            _initialized = true;
            await _viewModel.InitAsync(CampaignId, DocumentId, FromCampaignId);
        }
    }
}
