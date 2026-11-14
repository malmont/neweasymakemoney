namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(FindingId), "FindingId")]
    public partial class LossFindingPage : ContentPage
    {
        private readonly LossFindingViewModel _viewModel;

        public LossFindingPage(LossFindingViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public string FindingId
        {
            set => _viewModel.FindingId = int.TryParse(value, out var id) ? id : 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadAsync();
        }
    }
}
