namespace Easymakemoney.Views.Statistiques
{
    public partial class StockPage : ContentPage
    {
        public StockPage(StatistiquesDataValueViewmodel viewModel)
        {
            InitializeComponent();
            this.BindingContext = viewModel;
        }
    }
}