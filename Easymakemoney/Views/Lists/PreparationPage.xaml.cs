namespace Easymakemoney.Views.Lists
{
    public partial class PreparationPage : ContentPage
    {
        private readonly PreparationViewModel _viewModel;

        public PreparationPage(PreparationViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.StartAsync();
        }
    }
}
