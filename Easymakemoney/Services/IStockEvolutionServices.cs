namespace Easymakemoney.Services
{
    public interface IStockEvolutionServices
    {
        Task<StockEvolution> GetStockEvolution();
        Task<StockValue> GetStockValueEvolution();
    }
}