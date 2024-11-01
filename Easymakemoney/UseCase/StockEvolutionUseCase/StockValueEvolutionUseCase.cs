namespace Easymakemoney.UseCase.StockEvolutionUseCase
{
    public class StockValueEvolutionUseCase 
    {
        private readonly IStockEvolutionServices _stockEvolutionServices;

        public StockValueEvolutionUseCase(IStockEvolutionServices stockEvolutionServices)
        {
            _stockEvolutionServices = stockEvolutionServices;
        }

        public async Task<StockValueEvolution> GetStockValueEvolution()
        {
            return await _stockEvolutionServices.GetStockValueEvolution();
        }
    }
}