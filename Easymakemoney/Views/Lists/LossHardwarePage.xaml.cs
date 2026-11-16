namespace Easymakemoney.Views.Lists
{
    public partial class LossHardwarePage : ContentPage
    {
        private readonly LossHardwareViewModel _viewModel;

        public LossHardwarePage(LossHardwareViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.StartAsync();
        }

        protected override void OnDisappearing()
        {
            // Plus aucune lecture une fois l'écran quitté.
            _viewModel.Stop();
            base.OnDisappearing();
        }
    }
}
