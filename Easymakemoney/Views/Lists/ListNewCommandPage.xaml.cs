
namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(CollectionId), "CollectionId")]
    [QueryProperty(nameof(Closed), "Closed")]
    public partial class ListNewCommandPage : ContentPage
    {
        private readonly ListNewCommandViewModel _viewModel;

        public ListNewCommandPage(ListNewCommandViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public int CollectionId { get; set; }

        public bool Closed { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.CollectionId = CollectionId; 
            _viewModel.IsClosed = Closed;
            await _viewModel.GetListCommandAsync();
        }
    }
}
