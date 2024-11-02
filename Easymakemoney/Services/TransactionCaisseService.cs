namespace Easymakemoney.Services
{
    public class TransactionCaisseService: ITransactionCaisseService
    {
        private readonly IHttpService _httpService;
        private const string TransactionCaisseUrl = "https://backend-strapi.online/jeesign/api";

        public TransactionCaisseService(IHttpService httpService)
        {
            _httpService = httpService;
        }
      

        public async Task<List<TransactionCaisse>> GetTransactions()
        {
            var url = $"{TransactionCaisseUrl}/caisse/transactions";
            var transactions = await _httpService.GetAsync<List<TransactionCaisse>>(url);
            return transactions ?? null;
        }
       
    }
}