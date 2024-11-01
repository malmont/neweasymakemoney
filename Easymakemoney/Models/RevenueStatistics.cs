
namespace Easymakemoney.Models
{
    public class RevenueStatistics : IAllDataStatistics<DailyRevenueData, WeeklyRevenueData, MonthlyRevenueData>
    {
        // Propriétés explicites pour les revenus
        public double CurrentWeekRevenue { get; set; }
        public double LastWeekRevenue { get; set; }
        public List<DailyRevenueData> DailyRevenueForCurrentWeek { get; set; } = new();

        public double CurrentMonthRevenue { get; set; }
        public double LastMonthRevenue { get; set; }
        public List<WeeklyRevenueData> WeeklyRevenueForCurrentMonth { get; set; } = new();

        public double CurrentYearRevenue { get; set; }
        public double LastYearRevenue { get; set; }
        public List<MonthlyRevenueData> MonthlyRevenueForCurrentYear { get; set; } = new();

        // Mappage aux propriétés de l'interface IAllDataStatistics avec get et set
        public double CurrentWeekData
        {
            get => CurrentWeekRevenue;
            set => CurrentWeekRevenue = value;
        }

        public double LastWeekData
        {
            get => LastWeekRevenue;
            set => LastWeekRevenue = value;
        }

        public List<DailyRevenueData> DailyDataForCurrentWeek
        {
            get => DailyRevenueForCurrentWeek;
            set => DailyRevenueForCurrentWeek = value;
        }

        public double CurrentMonthData
        {
            get => CurrentMonthRevenue;
            set => CurrentMonthRevenue = value;
        }

        public double LastMonthData
        {
            get => LastMonthRevenue;
            set => LastMonthRevenue = value;
        }

        public List<WeeklyRevenueData> WeeklyDataForCurrentMonth
        {
            get => WeeklyRevenueForCurrentMonth;
            set => WeeklyRevenueForCurrentMonth = value;
        }

        public double CurrentYearData
        {
            get => CurrentYearRevenue;
            set => CurrentYearRevenue = value;
        }

        public double LastYearData
        {
            get => LastYearRevenue;
            set => LastYearRevenue = value;
        }

        public List<MonthlyRevenueData> MonthlyDataForCurrentYear
        {
            get => MonthlyRevenueForCurrentYear;
            set => MonthlyRevenueForCurrentYear = value;
        }
    }
}
