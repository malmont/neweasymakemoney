

namespace Easymakemoney.Models
{
    public class OrderStatistics 
    {
        [JsonProperty("currentWeekCountAchatClientComplétée")]
        public double CurrentWeekCountAchatClientCompletee { get; set; }

        [JsonProperty("lastWeekCountAchatClientComplétée")]
        public double LastWeekCountAchatClientCompletee { get; set; }

        [JsonProperty("dailyCountAchatClient_ComplétéeForCurrentWeek")]
        public List<DailyOrderData> DailyCountAchatClientCompleteeForCurrentWeek { get; set; } = new();

        [JsonProperty("currentWeekCountAchatClientLivrée")]
        public double CurrentWeekCountAchatClientLivree { get; set; }

        [JsonProperty("lastWeekCountAchatClientLivrée")]
        public double LastWeekCountAchatClientLivree { get; set; }

        [JsonProperty("dailyCountAchatClient_LivréeForCurrentWeek")]
        public List<DailyOrderData> DailyCountAchatClientLivreeForCurrentWeek { get; set; } = new();

        [JsonProperty("currentWeekCountAchatClientAnnulation")]
        public double CurrentWeekCountAchatClientAnnulation { get; set; }

        [JsonProperty("lastWeekCountAchatClientAnnulation")]
        public double LastWeekCountAchatClientAnnulation { get; set; }

        [JsonProperty("dailyCountAchatClient_AnnulationForCurrentWeek")]
        public List<DailyOrderData> DailyCountAchatClientAnnulationForCurrentWeek { get; set; } = new();

        [JsonProperty("currentWeekCountRetourClientComplétée")]
        public double CurrentWeekCountRetourClientCompletee { get; set; }

        [JsonProperty("lastWeekCountRetourClientComplétée")]
        public double LastWeekCountRetourClientCompletee { get; set; }

        [JsonProperty("dailyCountRetourClient_ComplétéeForCurrentWeek")]
        public List<DailyOrderData> DailyCountRetourClientCompleteeForCurrentWeek { get; set; } = new();

        [JsonProperty("currentWeekCountRetourClientLivrée")]
        public double CurrentWeekCountRetourClientLivree { get; set; }

        [JsonProperty("lastWeekCountRetourClientLivrée")]
        public double LastWeekCountRetourClientLivree { get; set; }

        [JsonProperty("dailyCountRetourClient_LivréeForCurrentWeek")]
        public List<DailyOrderData> DailyCountRetourClientLivreeForCurrentWeek { get; set; } = new();

        [JsonProperty("currentMonthCount")]
        public double CurrentMonthCount { get; set; }

        [JsonProperty("lastMonthCount")]
        public double LastMonthCount { get; set; }

        [JsonProperty("weeklyCountForCurrentMonth")]
        public List<WeeklyOrderData> WeeklyCountForCurrentMonth { get; set; } = new();

        [JsonProperty("currentYearCount")]
        public double CurrentYearCount { get; set; }

         [JsonProperty("lastYearCount")]
        public double LastYearCount { get; set; }

        [JsonProperty("monthlyCountForCurrentYear")]
        public List<MonthlyOrderData> MonthlyCountForCurrentYear { get; set; } = new();
    }
}
