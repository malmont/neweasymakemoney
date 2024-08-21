namespace Easymakemoney.Views.Lists
{
    public partial class ListNewFraisDePortPage : ContentPage
    {
        // private readonly ListNewFraisDePortViewModel _viewModel;
        public ListNewFraisDePortPage(ListNewFraisDePortViewModel viewModel)
        {
            BindingContext =viewModel;
            InitializeComponent();
        }
   
    }
}