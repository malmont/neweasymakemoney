namespace Easymakemoney.Views.Lists
{
     [QueryProperty(nameof(CollectionId), "CollectionId")]
    public partial class ListNewCommandPage : ContentPage
    {
        private readonly ListNewCommandViewModel _viewModel;

        public ListNewCommandPage(ListNewCommandViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

       
        public int CollectionId { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.GetListCommandAsync(CollectionId);
        }
    }
}
