
namespace Easymakemoney.Views.Statistiques
{
    public partial class StatistiquesPage : ContentPage
    {
        public StatistiquesPage(StatistiquesViewModel viewModel)
        {
            InitializeComponent();
            this.BindingContext = viewModel;
        }
    }
}