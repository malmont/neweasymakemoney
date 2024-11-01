
namespace Easymakemoney.Models
{
    public class StockEvolution
    {
        [JsonProperty("stock_evolution_week")]
        public List<StockEvolutionData> StockEvolutionWeek { get; set; } = new();

        [JsonProperty("stock_evolution_month")]
        public List<StockEvolutionData> StockEvolutionMonth { get; set; } = new();

        [JsonProperty("stock_evolution_year")]
        public List<StockEvolutionData> StockEvolutionYear { get; set; } = new();
    }
}
