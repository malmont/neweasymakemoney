using System.Collections.Generic;

namespace Easymakemoney.Models
{
    public interface IAllDataStatistics<TDaily, TWeekly, TMonthly>
        where TDaily : IStatistiqueData
        where TWeekly : IStatistiqueData
        where TMonthly : IStatistiqueData
    {
        double CurrentWeekData { get; set; }
        double LastWeekData { get; set; }
        List<TDaily> DailyDataForCurrentWeek { get; set; }
        
        double CurrentMonthData { get; set; }
        double LastMonthData { get; set; }
        List<TWeekly> WeeklyDataForCurrentMonth { get; set; }
        
        double CurrentYearData { get; set; }
        double LastYearData { get; set; }
        List<TMonthly> MonthlyDataForCurrentYear { get; set; }
    }
}
