namespace Easymakemoney.Services
{
    public class TransactionCaisseService: ITransactionCaisseService
    {
        private readonly IHttpService _httpService;

        public TransactionCaisseService(IHttpService httpService)
        {
            _httpService = httpService;
        }
      

        /// <summary>
        /// asList=1 (lot backend B7) : toujours 200 { cashRegisterId, sessionOpen, transactions }, même sans caisse ouverte.
        /// Sans ce paramètre, la route garde son 404 pour l'application de caisse.
        /// </summary>
        public async Task<List<TransactionCaisse>> GetTransactions()
        {
            var result = await _httpService.GetAsync<CaisseTransactionsList>("caisse/transactions?asList=1");
            return result?.transactions ?? new List<TransactionCaisse>();
        }

        private sealed class CaisseTransactionsList
        {
            public int? cashRegisterId { get; set; }
            public bool sessionOpen { get; set; }
            public List<TransactionCaisse>? transactions { get; set; }
        }
       
    }
}