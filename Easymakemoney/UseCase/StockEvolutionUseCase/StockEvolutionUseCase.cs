namespace Easymakemoney.UseCase.StockEvolutionUseCase
{
    public class StockEvolutionUseCase 
    {
        private readonly IStockEvolutionServices _stockEvolutionServices;

        public StockEvolutionUseCase(IStockEvolutionServices stockEvolutionServices)
        {
            _stockEvolutionServices = stockEvolutionServices;
        }

        public async Task<StockEvolution> GetStockEvolution()
        {
            return await _stockEvolutionServices.GetStockEvolution();
        }
    }
}