namespace Easymakemoney.Views.Statistiques

{
    public partial class PanierMoyenPage: ContentPage
    {
        public PanierMoyenPage(StatistiquesDataValueViewmodel viewModel)
        {
            InitializeComponent();
            // var data = useCase.GetData();
            // viewModel.LoadData(data);
            this.BindingContext = viewModel;
        }
    }
}