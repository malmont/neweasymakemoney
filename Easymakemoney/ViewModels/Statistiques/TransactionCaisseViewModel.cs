

namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class TransactionCaisseViewModel : BaseViewModel
    {
      
        [ObservableProperty] private List<TransactionCaisse> allTransactions;

        public TransactionCaisseViewModel()
        {
       
        }

        public async Task LoadTransactionsAsync(List<TransactionCaisse> transactions)
        {
            AllTransactions = transactions;
            ApplyAlternateRowColors(AllTransactions);
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
