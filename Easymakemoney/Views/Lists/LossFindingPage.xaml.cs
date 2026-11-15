namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(FindingId), "FindingId")]
    [QueryProperty(nameof(Exercise), "Exercise")]
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

        public string Exercise
        {
            set => _viewModel.IsExercise = value == "1";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadAsync();
        }
    }
}
