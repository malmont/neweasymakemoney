namespace Easymakemoney.Views.Lists
{
    public partial class BugReportPage : ContentPage
    {
        private readonly BugReportViewModel _viewModel;

        public BugReportPage(BugReportViewModel viewModel)
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
