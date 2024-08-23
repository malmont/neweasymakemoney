namespace Easymakemoney.Views.Dasboard
{
     [QueryProperty(nameof(CommandId), "CommandId")]
    public partial class DashBoardProductPage : ContentPage
    {
        private readonly DashBoardProductViewModel _viewModel;
        public DashBoardProductPage(DashBoardProductViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public int CommandId { get; set; }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.CommandId = CommandId; 
          
        }
   
    }
}