
namespace Easymakemoney.Models
{
    public class StockValueEvolution
    {
        [JsonProperty("stock_value_current_month")]
        public List<StockValueData> StockValueCurrentMonth { get; set; } = new();

    }

    public class StockValue
    {
        [JsonProperty("stock_value_current")]
        public StockValueEvolution StockValueCurrent { get; set; } = new();

    }
}
