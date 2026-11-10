namespace Easymakemoney.Views.Lists
{
    public partial class TreasuryMovementPage : ContentPage
    {
        public TreasuryMovementPage(TreasuryMovementViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
