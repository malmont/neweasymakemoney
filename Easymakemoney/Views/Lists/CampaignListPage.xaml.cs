namespace Easymakemoney.Views.Lists
{
    public partial class CampaignListPage : ContentPage
    {
        private readonly CampaignListViewModel _viewModel;

        public CampaignListPage(CampaignListViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Relue à chaque retour : une campagne créée, modifiée ou supprimée ailleurs apparaît tout de suite.
            await _viewModel.LoadFirstPageAsync();
        }
    }
}
