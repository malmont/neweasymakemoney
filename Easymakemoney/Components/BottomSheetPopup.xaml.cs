namespace Easymakemoney.Components
{
    public partial class BottomSheetPopup : Popup
    {
        public BottomSheetPopup(CreateCollectionUseCase createCollectionUseCase, IPreferenceService preferenceService, ListNewCollectionViewModel parentViewModel)
        {
            InitializeComponent();
            BindingContext = new BottomSheetPopupViewModel(createCollectionUseCase, preferenceService, this, parentViewModel);

        }
    }
}