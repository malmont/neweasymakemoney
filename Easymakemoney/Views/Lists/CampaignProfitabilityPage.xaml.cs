namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(CampaignId), "CampaignId")]
    public partial class CampaignProfitabilityPage : ContentPage
    {
        private readonly CampaignProfitabilityViewModel _viewModel;
        private bool _initialized;

        public CampaignProfitabilityPage(CampaignProfitabilityViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public int CampaignId { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_initialized)
            {
                // Retour de la modification : la mesure est recalculée.
                await _viewModel.RefreshAsync();
                return;
            }

            _initialized = true;
            await _viewModel.InitAsync(CampaignId);
        }
    }
}
