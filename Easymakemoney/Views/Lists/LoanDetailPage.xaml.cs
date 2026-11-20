namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(LoanId), "LoanId")]
    public partial class LoanDetailPage : ContentPage
    {
        private readonly LoanDetailViewModel _viewModel;

        public LoanDetailPage(LoanDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public int LoanId { get; set; }

        /// <summary>Relue à chaque retour : après « Modifier », la fiche et l'échéancier sont recalculés.</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadAsync(LoanId);
        }
    }
}
