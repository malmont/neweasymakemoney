namespace Easymakemoney.Services
{

    public class StockEvolutionServices : IStockEvolutionServices
    {
        private readonly IHttpService _httpService;

        public StockEvolutionServices(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<StockEvolution> GetStockEvolution()
        {
            var url = "stock-evolution";
            var stockEvolution = await _httpService.GetAsync<StockEvolution>(url);
            return stockEvolution ?? null;
        }

        public async Task<StockValue> GetStockValueEvolution()
        {
            var url = "stock-value";
            var stockValueEvolution = await _httpService.GetAsync<StockValue>(url);
            return stockValueEvolution ?? null;
        }



    }
}