namespace Easymakemoney.Views.Dasboard
{
    public partial class DashBoardCollectionPage : ContentPage
    {
        public DashBoardCollectionPage(DashBoardCollectionViewModel viewModel)
        {
            InitializeComponent();
            this.BindingContext = viewModel;
        }
    }
}
