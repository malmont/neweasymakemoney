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

        /// <summary>Panier moyen de l'année civile précédente, en cents (lot backend B3).</summary>
        [JsonProperty("lastYearAverage")]
        public double? LastYearAverage { get; set; }

        public double LastYearData
        {
            get => LastYearAverage ?? 0;
            set => LastYearAverage = value;
        }

        public List<MonthlyAverageOrderValueData> MonthlyDataForCurrentYear
        {
            get => MonthlyAverageForCurrentYear;
            set => MonthlyAverageForCurrentYear = value;
        }
    }
}
