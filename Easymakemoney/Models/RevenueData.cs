// Code antérieur aux annotations de nullité : avertissements coupés ici, sans effet sur l'exécution.
#nullable disable warnings


using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    public class DailyRevenueData : IStatistiqueData
    {
        public string Date { get; set; }
        public double Revenue { get; set; }

        private Color _backgroundColor = Colors.LightGray.WithAlpha(0.3f); // Valeur par défaut

        public Color BackgroundColor 
        { 
            get => _backgroundColor; 
            set => _backgroundColor = value; 
        }

        public string Data1 => Date;
        public double Data2 => Revenue;
    }

     public class WeeklyRevenueData : IStatistiqueData
    {
        public int Week { get; set; }
        public double Revenue { get; set; }

        private Color _backgroundColor = Colors.LightGray.WithAlpha(0.3f); // Valeur par défaut

        public Color BackgroundColor 
        { 
            get => _backgroundColor; 
            set => _backgroundColor = value; 
        }

        public string Data1 => $"Semaine {Week}";
        public double Data2 => Revenue;
    }

    public class MonthlyRevenueData : IStatistiqueData
    {
        public string Month { get; set; }
        public double Revenue { get; set; }

        private Color _backgroundColor = Colors.LightGray.WithAlpha(0.3f); // Valeur par défaut

        public Color BackgroundColor 
        { 
            get => _backgroundColor; 
            set => _backgroundColor = value; 
        }

        public string Data1 => MonthHelper.FormatMonthName(Month);
        public double Data2 => Revenue;
    }

     public class DailyOrderData : IStatistiqueData
    {
        public string Date { get; set; }
        public int OrderCount { get; set; }

        public Color BackgroundColor { get; set; } = Colors.LightGray.WithAlpha(0.3f);

        public string Data1 => Date;
        public double Data2 => OrderCount;
    }
     public class WeeklyOrderData : IStatistiqueData
    {
        public int Week { get; set; }
        public int OrderCount { get; set; }

        public Color BackgroundColor { get; set; } = Colors.LightGray.WithAlpha(0.3f);

        public string Data1 => $"Semaine {Week}";
        public double Data2 => OrderCount;
    }

      public class MonthlyOrderData : IStatistiqueData
    {
        public string Month { get; set; }
        public int OrderCount { get; set; }

        public Color BackgroundColor { get; set; } = Colors.LightGray.WithAlpha(0.3f);

        public string Data1 => MonthHelper.FormatMonthName(Month);
        public double Data2 => OrderCount;
    }

     public class DailyAverageOrderValueData : IStatistiqueData
    {
        public string Date { get; set; }
        public double AverageOrderValue { get; set; }

        public Color BackgroundColor { get; set; } = Colors.LightGray.WithAlpha(0.3f);

        public string Data1 => Date;
        public double Data2 => AverageOrderValue;
    }

    public class WeeklyAverageOrderValueData : IStatistiqueData
    {
        public int Week { get; set; }
        public double AverageOrderValue { get; set; }

        public Color BackgroundColor { get; set; } = Colors.LightGray.WithAlpha(0.3f);

        public string Data1 => $"Semaine {Week}";
        public double Data2 => AverageOrderValue;
    }

    public class MonthlyAverageOrderValueData : IStatistiqueData
    {
        public string Month { get; set; }
        public double AverageOrderValue { get; set; }

        public Color BackgroundColor { get; set; } = Colors.LightGray.WithAlpha(0.3f);

        public string Data1 => MonthHelper.FormatMonthName(Month);
        public double Data2 => AverageOrderValue;
    }

      public class StockEvolutionData : IStatistiqueData
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("quantity")]
        public double Quantity { get; set; }

        /// <summary>Code stable du type (in, out, return, adjustment, reception), envoyé par le backend depuis le lot B2.</summary>
        [JsonProperty("code")]
        public string? Code { get; set; }

        /// <summary>Libellé français : le libellé historique du backend peut être en anglais (« Return »).</summary>
        public string Label => Code switch
        {
            "in" => "Entrées",
            "out" => "Sorties (ventes)",
            "return" => "Retours",
            "adjustment" => "Ajustements",
            "reception" => "Réceptions fournisseur",
            _ => Type == "Return" ? "Retours" : Type,
        };

        public Color BackgroundColor { get; set; } = Colors.LightGray.WithAlpha(0.3f);

        public string Data1 => Label;
        public double Data2 => Quantity;
    }
}