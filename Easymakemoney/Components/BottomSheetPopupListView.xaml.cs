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
             // Assurez-vous que les items sont bien chargés
            Task.Run(async () => await viewModel.GetItemsAsync()).ConfigureAwait(false);
            // viewModel.CollectionId = CollectionId; 
            // viewModel.GetListCollectionAsync();
        }
    }
}
