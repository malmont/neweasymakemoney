namespace Easymakemoney.Components
{
    [QueryProperty(nameof(CollectionId), "CollectionId")]
    public partial class BottomSheetPopupListView : Popup
    {
        public int CollectionId { get; set; }

        public BottomSheetPopupListView(BottomSheetPopupListViewViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            viewModel.SetPopupInstance(this);
            viewModel.CollectionId = CollectionId; 
            viewModel.GetListCollectionAsync();
        }
    }
}
