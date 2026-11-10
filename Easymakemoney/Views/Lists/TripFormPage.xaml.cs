namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(TripId), "TripId")]
    public partial class TripFormPage : ContentPage
    {
        private readonly TripFormViewModel _viewModel;
        private int _tripId;
        private bool _loaded;

        public TripFormPage(TripFormViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public string TripId
        {
            set => _tripId = int.TryParse(value, out var id) ? id : 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_loaded)
                return;
            _loaded = true;
            await _viewModel.LoadAsync(_tripId);
        }
    }
}
