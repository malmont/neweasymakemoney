using System.Globalization;
using Microcharts;
using SkiaSharp;

namespace Easymakemoney.ViewModels.Statistiques
{
    public partial class StatistiquesDataValueViewmodel : BaseViewModel
    {
        // Propriétés de recettes et périodes
        [ObservableProperty] private double currentDataValueWeek;
        [ObservableProperty] private double lastDataValueWeek;
        [ObservableProperty] private double currentDataValueMonth;
        [ObservableProperty] private double lastDataValueMonth;
        [ObservableProperty] private double currentDataValueYear;
        [ObservableProperty] private double lastDataValueYear;
        [ObservableProperty] private double currentRevenue;
        [ObservableProperty] private double lastDataValue;
        [ObservableProperty] private string currentPeriode;
        [ObservableProperty] private string lastPeriode;
        [ObservableProperty] private string typeDataValue;

        // Collections de données pour différentes périodes
        [ObservableProperty] private List<IStatistiqueData> dailyDataValueCurrentWeek;
        [ObservableProperty] private List<IStatistiqueData> weeklyDataValueForCurrentMonth;
        [ObservableProperty] private List<IStatistiqueData> monthlyDataValueForCurrentYear;
        [ObservableProperty] private List<IStatistiqueData>? allDataValue;

        public int periodeTypeId { get; set; }
        public List<PeriodeType> periodeTypeChoices { get; set; }

        // Propriétés pour chaque graphique
        [ObservableProperty] private Chart weekChart;
        [ObservableProperty] private Chart monthChart;
        [ObservableProperty] private Chart yearChart;

        [ObservableProperty] bool isWeekChartVisible;

        [ObservableProperty] bool isMonthChartVisible;

        [ObservableProperty] bool isYearChartVisible;

        public StatistiquesDataValueViewmodel()
        {

            periodeTypeChoices = new List<PeriodeType>
            {
                new PeriodeType { id = 1, typePeriode = "Semaine", photoPeriode = "periode_semaine.png" },
                new PeriodeType { id = 2, typePeriode = "Mois", photoPeriode = "periode_mois.png" },
                new PeriodeType { id = 3, typePeriode = "Année", photoPeriode = "periode_annee.png" }
            };
            
        }
        public void LoadDataChiffreAffaire(RevenueStatistics data)
        {
            CurrentPeriode = "Semaine en cours";
            LastPeriode = "Semaine précédente";
            TypeDataValue = "Recette journalière";
            CurrentDataValueWeek = data.CurrentWeekRevenue;
            LastDataValueWeek = data.LastWeekRevenue;
            CurrentDataValueMonth = data.CurrentMonthRevenue;
            LastDataValueMonth = data.LastMonthRevenue;
            CurrentDataValueYear = data.CurrentYearRevenue;
            LastDataValueYear = data.LastYearRevenue;
            DailyDataValueCurrentWeek = data.DailyRevenueForCurrentWeek.Cast<IStatistiqueData>().ToList();
            WeeklyDataValueForCurrentMonth = data.WeeklyRevenueForCurrentMonth.Cast<IStatistiqueData>().ToList();
            MonthlyDataValueForCurrentYear = data.MonthlyRevenueForCurrentYear.Cast<IStatistiqueData>().ToList();

            // Appliquer les couleurs alternées
            ApplyAlternateRowColors(DailyDataValueCurrentWeek);
            ApplyAlternateRowColors(WeeklyDataValueForCurrentMonth);
            ApplyAlternateRowColors(MonthlyDataValueForCurrentYear);
        }


        public void UpdateRevenueData(int periodeTypeId)
        {

            IsWeekChartVisible = false;
            IsMonthChartVisible = false;
            IsYearChartVisible = false;
            // Assurez-vous de créer des graphiques distincts pour chaque période
            switch (periodeTypeId)
            {
                case 1: // Semaine
                    AllDataValue = dailyDataValueCurrentWeek;
                    CurrentRevenue = currentDataValueWeek;
                    LastDataValue = lastDataValueWeek;
                    CurrentPeriode = "Semaine en cours";
                    LastPeriode = "Semaine précédente";
                    TypeDataValue = "Recette journalière";
                    WeekChart = GenerateChart(AllDataValue);
                    IsWeekChartVisible = true;
                    break;

                case 2: // Mois
                    AllDataValue = weeklyDataValueForCurrentMonth;
                    CurrentRevenue = currentDataValueMonth;
                    LastDataValue = lastDataValueMonth;
                    CurrentPeriode = "Mois en cours";
                    LastPeriode = "Mois précédent";
                    TypeDataValue = "Recette mensuelle";
                    MonthChart = GenerateChart(AllDataValue);
                    IsMonthChartVisible = true;
                    break;

                case 3: // Année
                    AllDataValue = monthlyDataValueForCurrentYear;
                    CurrentRevenue = currentDataValueYear;
                    LastDataValue = lastDataValueYear;
                    CurrentPeriode = "Année en cours";
                    LastPeriode = "Année précédente";
                    TypeDataValue = "Recette annuelle";
                    YearChart = GenerateChart(AllDataValue);
                    IsYearChartVisible = true;
                    break;
            }
        }

        private Chart GenerateChart(List<IStatistiqueData> revenueData)
        {
            var entries = new List<ChartEntry>();
            int totalItems = revenueData.Count;

            for (int i = 0; i < totalItems; i++)
            {
                var data = revenueData[i];
                SKColor color = GenerateColor(i, totalItems);

                string label = DateTime.TryParseExact(data.Data1, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                    ? date.ToString("dd/MM")
                    : data.Data1;

                entries.Add(new ChartEntry(Convert.ToSingle(data.Data2))
                {
                    Label = label,
                    ValueLabel = (data.Data2 / 100).ToString("C"),
                    Color = color
                });
            }

            return new PointChart
            {
                Entries = entries,
                BackgroundColor = SKColors.Transparent,
                LabelTextSize = 30,
                PointSize = 15
            };
        }

        private SKColor GenerateColor(int index, int totalItems)
        {
            float hue = (360f / totalItems) * index;
            return SKColor.FromHsv(hue, 70, 90);
        }

        [ICommand]
        public async Task ShowBottomSheetPopupListView()
        {
            var viewModel = new BottomSheetPopupListViewViewModel(
                getItemsFunc: async () => await Task.FromResult(periodeTypeChoices),
                onItemTappedAction: async (item) =>
                {
                    if (item is PeriodeType periodeType)
                    {
                        periodeTypeId = periodeType.id;
                        UpdateRevenueData(periodeTypeId);
                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            await Application.Current.MainPage.ShowPopupAsync(popup);
        }

        private void ApplyAlternateRowColors(List<IStatistiqueData> dataList)
        {
            for (int i = 0; i < dataList.Count; i++)
            {
                dataList[i].BackgroundColor = i % 2 == 0
                    ? Colors.LightGray.WithAlpha(0.3f)
                    : Colors.Transparent;
            }
        }
    }
}
