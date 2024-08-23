namespace Easymakemoney.Views.Dasboard
{
    [QueryProperty(nameof(CollectionId), "CollectionId")]
    public partial class DashBoardCommandPage : ContentPage
    {
        private readonly DashBoardCommandViewModel _viewModel;
        public DashBoardCommandPage(DashBoardCommandViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }
        public int CollectionId { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.CollectionId = CollectionId; 
          
        }
    }
}