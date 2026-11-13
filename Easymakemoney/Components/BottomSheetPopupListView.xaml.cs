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
            // La liste liée à l'écran se remplit sur le fil de l'interface, jamais depuis une tâche d'arrière-plan.
            Dispatcher.Dispatch(async () => await viewModel.GetItemsAsync());

        }
    }
}
