
namespace Easymakemoney.Models
{
    public class FraisEvolution
    {
        [JsonProperty("total_frais_annee")]
        public AnnualFrais TotalFraisAnnee { get; set; }

        [JsonProperty("frais_par_mois")]
        public List<MonthlyFraisData> FraisParMois { get; set; } = new();

        [JsonProperty("frais_par_jour")]
        public List<DailyFraisData> FraisParJour { get; set; } = new();

        public class AnnualFrais
        {
            [JsonProperty("year")]
            public int Year { get; set; }

            [JsonProperty("total_frais")]
            public FraisDetails TotalFrais { get; set; }
        }


        public class MonthlyFraisData : IFraisData
        {
            [JsonProperty("month")]
            public string Month { get; set; }

            [JsonProperty("total_frais")]
            public FraisDetails TotalFrais { get; set; }

            public string PeriodLabel => Month;
            private Color _backgroundColor = Colors.LightGray.WithAlpha(0.3f);

            public Color BackgroundColor
            {
                get => _backgroundColor;
                set => _backgroundColor = value;
            }
        }

        public class DailyFraisData : IFraisData
        {
            [JsonProperty("date")]
            public string Date { get; set; }

            [JsonProperty("total_frais")]
            public FraisDetails TotalFrais { get; set; }
            public string PeriodLabel => Date;
            private Color _backgroundColor = Colors.LightGray.WithAlpha(0.3f);

            public Color BackgroundColor
            {
                get => _backgroundColor;
                set => _backgroundColor = value;
            }

        }


        public class FraisDetails
        {
            [JsonProperty("total")]
            public double Total { get; set; }

            [JsonProperty("note_de_frais")]
            public double NoteDeFrais { get; set; }

            [JsonProperty("frais_de_port")]
            public double FraisDePort { get; set; }
        }
    }
}
