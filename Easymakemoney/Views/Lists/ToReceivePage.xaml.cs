namespace Easymakemoney.Views.Lists
{
    public partial class ToReceivePage : ContentPage
    {
        private readonly ToReceiveViewModel _viewModel;

        public ToReceivePage(ToReceiveViewModel viewModel)
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
