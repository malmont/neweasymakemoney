namespace Easymakemoney.Views.Lists
{
    public partial class ListNewFournisseurPage : ContentPage
    {
        private readonly ListNewFournisseurViewModel _viewModel;
        public ListNewFournisseurPage(ListNewFournisseurViewModel viewModel)
        {
            BindingContext =_viewModel=viewModel;
            InitializeComponent();
        }

         protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.GetListFournisseursAsync();
        }
    }
}