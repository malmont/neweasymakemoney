using System.Collections.Generic;

namespace Easymakemoney.Models
{
    public class AverageOrderValueStatistics : IAllDataStatistics<DailyAverageOrderValueData, WeeklyAverageOrderValueData, MonthlyAverageOrderValueData>
    {
        // Propriétés explicites pour les valeurs moyennes de commande, avec les noms de l'API
        public double CurrentWeekAverage { get; set; }
        public double LastWeekAverage { get; set; }
        public List<DailyAverageOrderValueData> DailyAverageForCurrentWeek { get; set; } = new();

        public double CurrentMonthAverage { get; set; }
        public double LastAverageCount { get; set; }  // Propriété API pour la moyenne du mois précédent
        public List<WeeklyAverageOrderValueData> WeeklyAverageForCurrentMonth { get; set; } = new();

        public double CurrentYearAverage { get; set; }
        public List<MonthlyAverageOrderValueData> MonthlyAverageForCurrentYear { get; set; } = new();

        // Mappage aux propriétés de l'interface IAllDataStatistics
        public double CurrentWeekData
        {
            get => CurrentWeekAverage;
            set => CurrentWeekAverage = value;
        }

        public double LastWeekData
        {
            get => LastWeekAverage;
            set => LastWeekAverage = value;
        }

        public List<DailyAverageOrderValueData> DailyDataForCurrentWeek
        {
            get => DailyAverageForCurrentWeek;
            set => DailyAverageForCurrentWeek = value;
        }

        public double CurrentMonthData
        {
            get => CurrentMonthAverage;
            set => CurrentMonthAverage = value;
        }

        public double LastMonthData
        {
            get => LastAverageCount;
            set => LastAverageCount = value;
        }

        public List<WeeklyAverageOrderValueData> WeeklyDataForCurrentMonth
        {
            get => WeeklyAverageForCurrentMonth;
            set => WeeklyAverageForCurrentMonth = value;
        }

        public double CurrentYearData
        {
            get => CurrentYearAverage;
            set => CurrentYearAverage = value;
        }

        public double LastYearData // Propriété vide car l'API ne retourne pas la dernière moyenne annuelle
        {
            get => 0;
            set { } 
        }

        public List<MonthlyAverageOrderValueData> MonthlyDataForCurrentYear
        {
            get => MonthlyAverageForCurrentYear;
            set => MonthlyAverageForCurrentYear = value;
        }
    }
}
