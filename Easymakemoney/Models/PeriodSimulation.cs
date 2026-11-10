using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>Simulation d'une période (POST periodes/{id}/simulation, lot backend B1b-2, docs/simulateur-gestion.md). Rien n'est écrit.</summary>
    public class PeriodSimulation
    {
        public SimulationPeriod? period { get; set; }
        public string? currency { get; set; }
        public string? method { get; set; }
        public SimulationLastYear? lastYear { get; set; }
        public SimulationScenario? @base { get; set; }
        public SimulationScenario? simulated { get; set; }
        public SimulationScenario? delta { get; set; }
        public SimulationPlanned? plannedToSave { get; set; }
        public List<TreasuryCaveat> caveats { get; set; } = new();

        public string MethodText => method switch
        {
            "pace" => "Ventes à venir estimées au rythme des 30 derniers jours.",
            "lastYear" => "Ventes à venir estimées sur la même période l'an dernier.",
            _ => "Pas assez de ventes pour estimer la suite de la période.",
        };

        public string PeriodText => period == null
            ? string.Empty
            : $"{period.name} · {period.daysRemaining} jours restants sur {period.daysTotal}";

        public string LastYearText => lastYear == null
            ? string.Empty
            : $"L'an dernier, même période : {Money(lastYear.revenue)} de ventes, {Money(lastYear.grossMargin)} de marge.";

        public bool HasLastYear => lastYear != null;

        public string CaveatsText => string.Join("\n", caveats.Where(caveat => !string.IsNullOrWhiteSpace(caveat.text)).Select(caveat => $"• {caveat.text}"));
        public bool HasCaveats => CaveatsText.Length > 0;

        /// <summary>Lignes de comparaison actuel / simulé, dans l'ordre de lecture d'un compte de résultat.</summary>
        public List<SimulationRow> Rows()
        {
            var b = @base ?? new SimulationScenario();
            var s = simulated ?? new SimulationScenario();
            return new List<SimulationRow>
            {
                Row("Ventes de la période", b.sales.total, s.sales.total, higherIsBetter: true),
                Row("Dont ventes à venir", b.sales.projected, s.sales.projected, higherIsBetter: true),
                Row("Marge brute", b.grossMargin, s.grossMargin, higherIsBetter: true),
                RateRow("Taux de marque", b.tauxMarque, s.tauxMarque),
                Row("Frais de la période", b.expenses.total, s.expenses.total, higherIsBetter: false),
                Row("Résultat", b.result, s.result, higherIsBetter: true, isTotal: true),
                Row("Point mort (ventes nécessaires)", b.breakEven?.revenue, s.breakEven?.revenue, higherIsBetter: false),
                Row("Stock restant en fin de période", b.stockEnd.saleValue, s.stockEnd.saleValue, higherIsBetter: null),
                Row("Trésorerie après achats", b.cashAfterPurchases, s.cashAfterPurchases, higherIsBetter: true),
            };
        }

        public string Money(long? cents) => cents is { } value ? SiteCurrency.FormatCents(value, null, currency) : "—";

        private SimulationRow Row(string label, long? baseValue, long? simulatedValue, bool? higherIsBetter, bool isTotal = false)
        {
            long? difference = baseValue.HasValue && simulatedValue.HasValue ? simulatedValue - baseValue : null;
            return new SimulationRow
            {
                Label = label,
                BaseText = Money(baseValue),
                SimulatedText = Money(simulatedValue),
                DeltaText = difference is null or 0 ? string.Empty : (difference > 0 ? "+" : "−") + Money(Math.Abs(difference.Value)),
                Tone = Tone(difference, higherIsBetter),
                IsTotal = isTotal,
            };
        }

        private static SimulationRow RateRow(string label, double? baseRate, double? simulatedRate)
        {
            double? difference = baseRate.HasValue && simulatedRate.HasValue ? Math.Round(simulatedRate.Value - baseRate.Value, 2) : null;
            return new SimulationRow
            {
                Label = label,
                BaseText = Rate(baseRate),
                SimulatedText = Rate(simulatedRate),
                DeltaText = difference is null or 0 ? string.Empty : (difference > 0 ? "+" : "−") + Math.Abs(difference.Value).ToString("0.##", CultureInfo.CurrentCulture) + " pt",
                Tone = difference is null or 0 ? 0 : difference > 0 ? 1 : -1,
            };
        }

        private static string Rate(double? rate) => rate is { } value ? value.ToString("0.##", CultureInfo.CurrentCulture) + " %" : "—";

        private static int Tone(long? difference, bool? higherIsBetter)
        {
            if (difference is null or 0 || higherIsBetter is null)
                return 0;
            return (difference > 0) == higherIsBetter ? 1 : -1;
        }
    }

    public class SimulationRow
    {
        public string Label { get; init; } = string.Empty;
        public string BaseText { get; init; } = string.Empty;
        public string SimulatedText { get; init; } = string.Empty;
        public string DeltaText { get; init; } = string.Empty;

        /// <summary>1 : mieux, −1 : moins bien, 0 : neutre.</summary>
        public int Tone { get; init; }
        public bool IsTotal { get; init; }
        public bool HasDelta => DeltaText.Length > 0;

        public Microsoft.Maui.Graphics.Color DeltaColor => Microsoft.Maui.Graphics.Color.FromArgb(Tone switch
        {
            1 => "#047857",
            -1 => "#B91C1C",
            _ => "#64748B",
        });
    }

    public class SimulationPeriod
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? start { get; set; }
        public string? end { get; set; }
        public int daysTotal { get; set; }
        public int daysElapsed { get; set; }
        public int daysRemaining { get; set; }
    }

    public class SimulationLastYear
    {
        public long revenue { get; set; }
        public long grossMargin { get; set; }
        public long units { get; set; }
    }

    public class SimulationScenario
    {
        public SimulationSales sales { get; set; } = new();
        public long? cogs { get; set; }
        public long? unknownCostRevenue { get; set; }
        public long? grossMargin { get; set; }
        public double? tauxMarque { get; set; }
        public SimulationExpenses expenses { get; set; } = new();
        public long? result { get; set; }
        public SimulationBreakEven? breakEven { get; set; }
        public SimulationStock stockEnd { get; set; } = new();
        public long? cashAfterPurchases { get; set; }
    }

    public class SimulationSales
    {
        public long? realized { get; set; }
        public long? projected { get; set; }
        public long? total { get; set; }
    }

    public class SimulationExpenses
    {
        public long? total { get; set; }
    }

    public class SimulationBreakEven
    {
        public long? revenue { get; set; }
        public bool? reached { get; set; }
    }

    public class SimulationStock
    {
        public long? units { get; set; }
        public long? costValue { get; set; }
        public long? saleValue { get; set; }
    }

    public class SimulationPlanned
    {
        public List<SimulationPlannedLine> lines { get; set; } = new();
    }

    public class SimulationPlannedLine
    {
        public string? category { get; set; }
        public long planned { get; set; }
    }

    /// <summary>Leviers envoyés au simulateur ; un levier nul n'est pas envoyé.</summary>
    public sealed class SimulationLevers
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double? pricePercent { get; init; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SimulationPromotion? promotion { get; init; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long? advertisingDelta { get; init; }
    }

    public sealed class SimulationPromotion
    {
        public double percent { get; init; }
        public string scope { get; init; } = "all";
    }
}
