using Newtonsoft.Json;

namespace Easymakemoney.Models
{
    public class StockValueData
    {
        [JsonProperty("month")]
        public string Month { get; set; }

        [JsonProperty("stock_value")]
        public double StockValue { get; set; }
    }
}
