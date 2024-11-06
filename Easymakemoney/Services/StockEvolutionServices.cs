namespace Easymakemoney.Services
{

    public class StockEvolutionServices : IStockEvolutionServices
    {
        private const string StockEvolutionsUrl = "https://backend-strapi.online/jeesign/api";

        private readonly IHttpService _httpService;

        public StockEvolutionServices(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<StockEvolution> GetStockEvolution()
        {
            var url = $"{StockEvolutionsUrl}/stock-evolution";
            var stockEvolution = await _httpService.GetAsync<StockEvolution>(url);
            return stockEvolution ?? null;
        }

        public async Task<StockValue> GetStockValueEvolution()
        {
            var url = $"{StockEvolutionsUrl}/stock-value";
            var stockValueEvolution = await _httpService.GetAsync<StockValue>(url);
            return stockValueEvolution ?? null;
        }



    }
}