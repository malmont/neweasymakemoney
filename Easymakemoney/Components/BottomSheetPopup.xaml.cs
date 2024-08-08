namespace Easymakemoney.Components
{
    public partial class BottomSheetPopup : Popup
    {
        public BottomSheetPopup(BottomSheetPopupViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
              viewModel.SetPopupInstance(this);
        }
    }
}
