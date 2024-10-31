namespace Easymakemoney.Views.Statistiques

{
    public partial class OrderNumberPage: ContentPage
    {
        public OrderNumberPage(StatistiquesDataValueViewmodel viewModel)
        {
            InitializeComponent();
            // var data = useCase.GetData();
            // viewModel.LoadData(data);
            this.BindingContext = viewModel;
        }
    }
}