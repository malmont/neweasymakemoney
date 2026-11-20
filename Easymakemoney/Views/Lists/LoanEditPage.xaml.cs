namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(LoanId), "LoanId")]
    public partial class LoanEditPage : ContentPage
    {
        private readonly LoanEditViewModel _viewModel;
        private bool _initialized;

        public LoanEditPage(LoanEditViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        /// <summary>0 : nouveau prêt.</summary>
        public int LoanId { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Retour d'un sélecteur de date : la saisie en cours est gardée.
            if (_initialized)
                return;

            _initialized = true;
            await _viewModel.InitAsync(LoanId);
        }
    }
}
