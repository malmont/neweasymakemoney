namespace Easymakemoney.Models
{
    public class OrderStatistics
    {
        public int CurrentWeekCountAchatClientCompletee { get; set; }
        public int LastWeekCountAchatClientCompletee { get; set; }
        public List<DailyOrderData> DailyCountAchatClientCompleteeForCurrentWeek { get; set; } = new();

        public int CurrentWeekCountAchatClientLivree { get; set; }
        public int LastWeekCountAchatClientLivree { get; set; }
        public List<DailyOrderData> DailyCountAchatClientLivreeForCurrentWeek { get; set; } = new();

        public int CurrentWeekCountAchatClientAnnulation { get; set; }
        public int LastWeekCountAchatClientAnnulation { get; set; }
        public List<DailyOrderData> DailyCountAchatClientAnnulationForCurrentWeek { get; set; } = new();

        public int CurrentWeekCountRetourClientCompletee { get; set; }
        public int LastWeekCountRetourClientCompletee { get; set; }
        public List<DailyOrderData> DailyCountRetourClientCompleteeForCurrentWeek { get; set; } = new();

        public int CurrentWeekCountRetourClientLivree { get; set; }
        public int LastWeekCountRetourClientLivree { get; set; }
        public List<DailyOrderData> DailyCountRetourClientLivreeForCurrentWeek { get; set; } = new();

        public int CurrentMonthCount { get; set; }
        public int LastMonthCount { get; set; }
        public List<WeeklyOrderData> WeeklyCountForCurrentMonth { get; set; } = new();

        public int CurrentYearCount { get; set; }
        public List<MonthlyOrderData> MonthlyCountForCurrentYear { get; set; } = new();
    }
}
