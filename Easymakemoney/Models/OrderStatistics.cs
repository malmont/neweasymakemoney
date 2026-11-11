

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

        // Lot backend B4 : ventes seules (type 1, hors incomplètes et annulées), comme le résultat de période.
        // Quand elles sont présentes, elles remplacent les anciens totaux, qui comptaient aussi les retours et les annulées.
        [JsonProperty("currentMonthSalesCount")]
        public double? CurrentMonthSalesCount { get; set; }

        [JsonProperty("lastMonthSalesCount")]
        public double? LastMonthSalesCount { get; set; }

        [JsonProperty("currentYearSalesCount")]
        public double? CurrentYearSalesCount { get; set; }

        [JsonProperty("lastYearSalesCount")]
        public double? LastYearSalesCount { get; set; }

        [JsonProperty("weeklySalesCountForCurrentMonth")]
        public List<WeeklyOrderData>? WeeklySalesCountForCurrentMonth { get; set; }

        [JsonProperty("monthlySalesCountForCurrentYear")]
        public List<MonthlyOrderData>? MonthlySalesCountForCurrentYear { get; set; }

        [System.Runtime.Serialization.OnDeserialized]
        internal void PreferSalesCounts(System.Runtime.Serialization.StreamingContext context)
        {
            if (CurrentMonthSalesCount is { } currentMonth) CurrentMonthCount = currentMonth;
            if (LastMonthSalesCount is { } lastMonth) LastMonthCount = lastMonth;
            if (CurrentYearSalesCount is { } currentYear) CurrentYearCount = currentYear;
            if (LastYearSalesCount is { } lastYear) LastYearCount = lastYear;
            if (WeeklySalesCountForCurrentMonth is { } weekly) WeeklyCountForCurrentMonth = weekly;
            if (MonthlySalesCountForCurrentYear is { } monthly) MonthlyCountForCurrentYear = monthly;
        }
    }
}
