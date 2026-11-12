// Code antérieur aux annotations de nullité : avertissements coupés ici, sans effet sur l'exécution.
#nullable disable warnings


using Easymakemoney.Services.Dialog;
using Easymakemoney.UseCase.TransactionCaisseUseCase;

namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class TransactionCaisseViewModel : BaseViewModel
    {
        private readonly IDialogService? _dialogService;
        private readonly TransactionCaisseUseCase? _transactionCaisseUseCase;

        [ObservableProperty] private List<TransactionCaisse> allTransactions;

        public TransactionCaisseViewModel(IDialogService? dialogService = null,
            TransactionCaisseUseCase? transactionCaisseUseCase = null)
        {
            _dialogService = dialogService;
            _transactionCaisseUseCase = transactionCaisseUseCase;
        }

        public async Task LoadTransactionsFromUseCaseAsync()
        {
            if (_transactionCaisseUseCase == null) return;
            try
            {
                var transactions = await _transactionCaisseUseCase.GetTransactions();
                await LoadTransactionsAsync(transactions);
            }
            catch (Exception ex)
            {
                if (_dialogService != null)
                    await _dialogService.ShowErrorAsync(ex);
            }
        }

        public async Task LoadTransactionsAsync(List<TransactionCaisse>? transactions)
        {
            if (transactions == null)
                return;

            AllTransactions = transactions;
            ApplyAlternateRowColors(AllTransactions);
            await Task.CompletedTask;
        }

        private void ApplyAlternateRowColors(List<TransactionCaisse> dataList)
        {
            for (int i = 0; i < dataList.Count; i++)
            {
                dataList[i].BackgroundColor = i % 2 == 0
                    ? Colors.LightGray.WithAlpha(0.3f)
                    : Colors.Transparent;
            }
        }
    }
}
