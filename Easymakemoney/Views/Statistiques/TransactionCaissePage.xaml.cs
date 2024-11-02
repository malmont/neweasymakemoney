


namespace Easymakemoney.Views.Statistiques
{
    public partial class TransactionCaissePage : ContentPage
    {
        private readonly TransactionCaisseViewModel _viewModel;
        private readonly TransactionCaisseUseCase _transactionCaisseUseCase;


        
        public TransactionCaissePage(TransactionCaisseViewModel viewModel, TransactionCaisseUseCase transactionCaisseUseCase)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _transactionCaisseUseCase = transactionCaisseUseCase;
            this.BindingContext = _viewModel;
        }
      
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var data = await _transactionCaisseUseCase.GetTransactions();
            await _viewModel.LoadTransactionsAsync(data);
            
        }
    }
}