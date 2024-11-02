
namespace Easymakemoney.Models
{
    public class PaiementData : IStatistiqueData
    {
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("amount")]
        public double Amount { get; set; }

        public string Data1 => Date;
        public double Data2 => Amount;
        public Color BackgroundColor { get; set; } = Colors.Transparent;
    }

    public class WeeklyPaiementDetails
    {
        [JsonProperty("total")]
        public double Total { get; set; }

        [JsonProperty("daily")]
        public List<PaiementData> Daily { get; set; } = new();
    }

    public class CurrentWeekData
    {
        [JsonProperty("PaiementClient")]
        public WeeklyPaiementDetails PaiementClient { get; set; }

        [JsonProperty("RemboursementClient")]
        public WeeklyPaiementDetails RemboursementClient { get; set; }
    }

    public class CurrentMonthData
    {
        [JsonProperty("PaiementClient")]
        public double PaiementClient { get; set; }

        [JsonProperty("RemboursementClient")]
        public double RemboursementClient { get; set; }
    }

    public class CurrentYearData
    {
        [JsonProperty("PaiementClient")]
        public double PaiementClient { get; set; }

        [JsonProperty("RemboursementClient")]
        public double RemboursementClient { get; set; }
    }

    public class PaiementRemboursementEvolution
    {
        [JsonProperty("current_week")]
        public CurrentWeekData? CurrentWeek { get; set; }

        [JsonProperty("current_month")]
        public CurrentMonthData? CurrentMonth { get; set; }

        [JsonProperty("current_year")]
        public CurrentYearData? CurrentYear { get; set; }
    }
}
