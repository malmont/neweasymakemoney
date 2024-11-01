
namespace Easymakemoney.Models
{
    public class StockValueEvolution
    {
        [JsonProperty("stock_value_current_month")]
        public List<StockValueData> StockValueCurrentMonth { get; set; } = new();

        [JsonProperty("stock_value_last_month")]
        public List<StockValueData> StockValueLastMonth { get; set; } = new();

        [JsonProperty("stock_value_two_months_ago")]
        public List<StockValueData> StockValueTwoMonthsAgo { get; set; } = new();
    }
}
