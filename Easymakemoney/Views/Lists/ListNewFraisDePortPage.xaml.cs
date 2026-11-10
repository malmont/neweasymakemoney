namespace Easymakemoney.Views.Lists
{
    public partial class ListNewFraisDePortPage : ContentPage
    {
        public ListNewFraisDePortPage(ListNewFraisDePortViewModel viewModel)
        {
            BindingContext = _viewModel = viewModel;
            InitializeComponent();
        }

        private readonly ListNewFraisDePortViewModel _viewModel;

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.RefreshAsync();
        }
   
    }
}