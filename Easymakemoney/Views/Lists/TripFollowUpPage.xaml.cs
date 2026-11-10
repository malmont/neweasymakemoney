namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(TripId), "TripId")]
    public partial class TripFollowUpPage : ContentPage
    {
        private readonly TripFollowUpViewModel _viewModel;

        public TripFollowUpPage(TripFollowUpViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public string TripId
        {
            set => _viewModel.TripId = int.TryParse(value, out var id) ? id : 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadAsync();
        }
    }
}
