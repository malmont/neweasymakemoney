namespace Easymakemoney.Services
{
    public interface ITransactionCaisseService
    {
        Task<List<TransactionCaisse>> GetTransactions();
    }
}