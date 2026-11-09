namespace Easymakemoney.Views.Lists
{
    public partial class ReprisePage : ContentPage
    {
        private readonly RepriseViewModel _viewModel;

        public ReprisePage(RepriseViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadAsync();
        }
    }
}
