namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(DocType), "Type")]
    [QueryProperty(nameof(CommandeId), "CommandeId")]
    [QueryProperty(nameof(Purpose), "Purpose")]
    public partial class DocumentReadPage : ContentPage
    {
        private readonly DocumentReadViewModel _viewModel;
        private bool _initialized;

        public DocumentReadPage(DocumentReadViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public string DocType { get; set; } = "note";

        public int CommandeId { get; set; }

        /// <summary>« campaign » : la facture lue préremplit le formulaire de campagne au lieu d'une note de frais.</summary>
        public string? Purpose { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Les sélecteurs de fichiers ramènent sur cette page : on n'initialise qu'une fois.
            if (_initialized)
                return;

            _initialized = true;
            await _viewModel.InitAsync(DocType, CommandeId, Purpose);
        }

        protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            if (!Navigation.NavigationStack.Contains(this))
                _viewModel.Stop();
        }
    }
}
