namespace Easymakemoney.Views.Lists
{
    public partial class ManagementReadinessPage : ContentPage
    {
        private readonly ManagementReadinessViewModel _viewModel;

        public ManagementReadinessPage(ManagementReadinessViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        // Relu à chaque retour : une étape réglée dans son écran passe aussitôt dans « Déjà fait ».
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadAsync();
        }
    }
}
