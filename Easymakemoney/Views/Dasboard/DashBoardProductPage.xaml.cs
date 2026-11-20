namespace Easymakemoney.Views.Dasboard
{
     [QueryProperty(nameof(CommandId), "CommandId")]
    [QueryProperty(nameof(Transfer), "Transfer")]
    public partial class DashBoardProductPage : ContentPage
    {
        private readonly DashBoardProductViewModel _viewModel;
        public DashBoardProductPage(DashBoardProductViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public int CommandId { get; set; }

        /// <summary>Commande d'arrivée d'un transfert du groupe (G-5).</summary>
        public bool Transfer { get; set; }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.CommandId = CommandId;
            _viewModel.IsGroupTransfer = Transfer;
          
        }
   
    }
}