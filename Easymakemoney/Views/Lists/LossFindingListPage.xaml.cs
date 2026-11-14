namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(Filter), "Filter")]
    public partial class LossFindingListPage : ContentPage
    {
        private readonly LossFindingListViewModel _viewModel;

        public LossFindingListPage(LossFindingListViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        /// <summary>« new » : la vignette « nouveaux constats » ouvre la liste des nouveaux.</summary>
        public string Filter
        {
            set => _viewModel.UseInitialFilter(value);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }
    }
}
