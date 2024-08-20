namespace Easymakemoney.Views.Lists
{
    public partial class ListNewNoteDeFraisPage : ContentPage
    {
        private readonly ListNewNoteDeFraisViewModel _viewModel;

        public ListNewNoteDeFraisPage(ListNewNoteDeFraisViewModel viewModel)
        {
            InitializeComponent(); // Ceci est auto-généré si tout est correct
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // await _viewModel.GetListNoteDeFraisAsync();
        }
      
    }
}