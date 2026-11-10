namespace Easymakemoney.Views.Lists
{
    public partial class StockLowPage : ContentPage
    {
        public StockLowPage(StockLowViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
