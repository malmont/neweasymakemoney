


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
      
        private readonly StatistiquesPageLoader _loader = new();

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _loader.RunOnceAsync(
                async () =>
                {
                    var data = await _transactionCaisseUseCase.GetTransactions();
                    await _viewModel.LoadTransactionsAsync(data);
                },
                // Pas de caisse ouverte, aucune transaction, ou plusieurs postes : le backend répond 404 ou 400.
                // Ce n'est pas une erreur : la liste reste vide, avec un message clair.
                async ex =>
                {
                    if (ex.StatusCode is not (System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.BadRequest))
                        return false;
                    await _viewModel.LoadTransactionsAsync(new List<TransactionCaisse>());
                    var message = ex.ApiError.Code == "CASH_REGISTER_REQUIRED"
                        ? "Plusieurs caisses existent : les transactions par caisse ne sont pas encore disponibles ici."
                        : "Aucune caisse ouverte, ou aucune transaction pour le moment.";
                    if (IPlatformApplication.Current?.Services.GetService<Easymakemoney.Services.Dialog.IDialogService>() is { } dialog)
                        await dialog.ShowInfoAsync(message, "Caisse");
                    return true;
                });
        }
    }
}