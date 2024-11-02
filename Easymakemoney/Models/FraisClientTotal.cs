
namespace Easymakemoney.Models
{

    public class MonthlyTaxData : IStatistiqueData
    {
        [JsonProperty("month")]
        public string Month { get; set; }

        [JsonProperty("total_tax")]
        public double TotalTax { get; set; }

        public string Data1 => Month;
        public double Data2 => TotalTax;
        public Color BackgroundColor { get; set; } = Colors.Transparent;
    }

    public class MonthlyCarrierData : IStatistiqueData
    {
        [JsonProperty("month")]
        public string Month { get; set; }

        [JsonProperty("carrier_count")]
        public double CarrierCount { get; set; }

        public string Data1 => Month;
        public double Data2 => CarrierCount;
        public Color BackgroundColor { get; set; } = Colors.Transparent;
    }

     public class CarrierStatisticsResponse
    {
        [JsonProperty("monthly_carrier_for_current_year")]
        public List<MonthlyCarrierData> MonthlyCarrierForCurrentYear { get; set; } = new();
    }

     public class TaxStatisticsResponse
    {
        [JsonProperty("monthly_taxes_for_current_year")]
        public List<MonthlyTaxData> MonthlyTaxesForCurrentYear { get; set; } = new();
    }

   
}
