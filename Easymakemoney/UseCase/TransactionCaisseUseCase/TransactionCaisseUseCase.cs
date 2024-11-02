namespace Easymakemoney.UseCase.TransactionCaisseUseCase
{
    public class TransactionCaisseUseCase
    {
        private readonly ITransactionCaisseService _transactionCaisseService;

        public TransactionCaisseUseCase(ITransactionCaisseService transactionCaisseService)
        {
            _transactionCaisseService = transactionCaisseService;
        }

        public async Task<List<TransactionCaisse>> GetTransactions()
        {
            return await _transactionCaisseService.GetTransactions();
        }
    }
}