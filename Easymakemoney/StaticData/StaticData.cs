namespace Easymakemoney.StaticData
{
    public static class StaticData
    {
        public static List<PeriodeType> PeriodeTypeChoices { get; } = new List<PeriodeType>
    {
        new PeriodeType { id = 1, typePeriode = "Semaine", photoPeriode = "week.png" },
                new PeriodeType { id = 2, typePeriode = "Mois", photoPeriode = "month.png" },
                new PeriodeType { id = 3, typePeriode = "Année", photoPeriode = "years.png" }
    };
    }
}