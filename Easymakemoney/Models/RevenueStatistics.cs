using System.Collections.Generic;

namespace Easymakemoney.Models
{
    public class RevenueStatistics
    {
        public double CurrentWeekRevenue { get; set; }
        public double LastWeekRevenue { get; set; }
        public List<DailyRevenueData> DailyRevenueForCurrentWeek { get; set; } = new();
        
        public double CurrentMonthRevenue { get; set; }
        public double LastMonthRevenue { get; set; }
        public List<WeeklyRevenueData> WeeklyRevenueForCurrentMonth { get; set; } = new();
        
        public double CurrentYearRevenue { get; set; }
        public double LastYearRevenue { get; set; }
        public List<MonthlyRevenueData> MonthlyRevenueForCurrentYear { get; set; } = new();
    }
}
