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
            var rows = new List<SimulationRow>
            {
                Row("Ventes de la période", b.sales.total, s.sales.total, higherIsBetter: true),
                Row("Dont ventes à venir", b.sales.projected, s.sales.projected, higherIsBetter: true),
                Row("Marge brute", b.grossMargin, s.grossMargin, higherIsBetter: true),
                RateRow("Taux de marque", b.tauxMarque, s.tauxMarque),
                Row("Frais de la période", b.expenses.total, s.expenses.total, higherIsBetter: false),
                Row("Résultat", b.result, s.result, higherIsBetter: true, isTotal: true),
                Row("Point mort (ventes nécessaires)", b.breakEven?.revenue, s.breakEven?.revenue, higherIsBetter: false),
            };
            // Lot FF2 : variante après frais fixes, seulement s'il y en a (le résultat principal ne change pas).
            if ((b.fixedCosts ?? 0) > 0 || (s.fixedCosts ?? 0) > 0)
            {
                rows.Add(Row("Frais fixes (part de la période)", b.fixedCosts, s.fixedCosts, higherIsBetter: false));
                rows.Add(Row("Résultat après frais fixes", b.resultAfterFixed, s.resultAfterFixed, higherIsBetter: true, isTotal: true));
                rows.Add(Row("Point mort après frais fixes", b.breakEvenAfterFixed?.revenue, s.breakEvenAfterFixed?.revenue, higherIsBetter: false));
            }
            rows.AddRange(new[]
            {
                Row("Stock restant en fin de période", b.stockEnd.saleValue, s.stockEnd.saleValue, higherIsBetter: null),
                Row("Trésorerie après achats", b.cashAfterPurchases, s.cashAfterPurchases, higherIsBetter: true),
            });
            foreach (var row in rows)
                row.Help = HelpFor(row.Label);
            return rows;
        }

        /// <summary>Définition affichée quand le gérant touche le titre d'une ligne.</summary>
        public static string HelpFor(string label) => label switch
        {
            "Ventes de la période" =>
                "Ce que la période vendra en tout, hors taxes : les ventes déjà faites, plus les ventes estimées d'ici la fin de la période.",
            "Dont ventes à venir" =>
                "La partie estimée des ventes : le rythme de vente des 30 derniers jours, prolongé jusqu'à la fin de la période, sans dépasser le stock disponible. Le reste est déjà vendu.",
            "Marge brute" =>
                "Ventes moins ce que ces articles vous ont coûté à l'achat.",
            "Taux de marque" =>
                "La part de marge dans chaque vente. À 43 %, sur 100 $ vendus, il vous reste 43 $ après le coût d'achat.",
            "Frais de la période" =>
                "Ce que la période vous coûte en plus des articles : voyage, frais de port, publicité et autres notes de frais. Pour chaque poste, le simulateur prend le plus grand entre le budget prévu et ce qui est déjà dépensé.",
            "Résultat" =>
                "Marge brute moins frais : ce que la période vous rapporte réellement. Un résultat négatif est une perte.",
            "Point mort (ventes nécessaires)" =>
                "Le montant de ventes à atteindre pour ne pas perdre d'argent sur la période : frais ÷ taux de marque. Au-dessus vous gagnez, en dessous vous perdez.",
            "Frais fixes (part de la période)" =>
                "Loyer, salaires, assurances, abonnements… comptés au prorata des jours de la période. Ils se saisissent dans Gestion › Frais fixes.",
            "Résultat après frais fixes" =>
                "Le résultat moins la part de frais fixes de la période : ce que l'activité vous rapporte une fois l'entreprise payée.",
            "Point mort après frais fixes" =>
                "Les ventes à atteindre pour couvrir les frais de la période et sa part de frais fixes : (frais + frais fixes) ÷ taux de marque.",
            "Stock restant en fin de période" =>
                "La valeur de vente du stock qu'il vous restera à la fin de la période, si le rythme de vente continue.",
            "Trésorerie après achats" =>
                "L'argent qu'il vous resterait en banque à la fin de la période, une fois payés les achats et les frais encore prévus. Un tiret : aucun solde n'est saisi dans Trésorerie.",
            _ => string.Empty,
        };

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

        /// <summary>Définition de la ligne (titre touchable).</summary>
        public string Help { get; set; } = string.Empty;

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

        /// <summary>Lot FF2 : frais fixes de la période au prorata, résultat et point mort après frais fixes.</summary>
        public long? fixedCosts { get; set; }
        public long? resultAfterFixed { get; set; }
        public SimulationBreakEven? breakEvenAfterFixed { get; set; }
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

        /// <summary>Seulement avec scope « products » (promotion conseillée sur des articles qui dorment).</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<int>? productIds { get; init; }
    }
}

namespace Easymakemoney.Models
{
    /// <summary>Avis du guide sur une simulation (POST periodes/{id}/simulation/commentaire, lot backend G1b-2).</summary>
    public class SimulationComment
    {
        public PeriodSimulation? simulation { get; set; }
        public List<SimulationCandidate> candidates { get; set; } = new();
        public SimulationCommentText? comment { get; set; }
        public AdviceAi? ai { get; set; }
        public bool cached { get; set; }

        /// <summary>Donne la devise et marque la répartition recommandée.</summary>
        public SimulationComment Prepare()
        {
            var currency = simulation?.currency;
            foreach (var candidate in candidates)
            {
                candidate.Currency = currency;
                candidate.IsRecommended = candidate.key == comment?.recommended;
            }
            return this;
        }

        public string SourceText => comment?.textSource == "ai" ? "Avis rédigé par l'IA à partir des chiffres ci-dessous." : "Avis calculé sur vos chiffres.";
    }

    public class SimulationCommentText
    {
        public string? recommended { get; set; }
        public string? title { get; set; }
        public string? text { get; set; }
        public string? textSource { get; set; }
    }

    public class SimulationCandidate
    {
        public string? key { get; set; }
        public string? label { get; set; }
        public Dictionary<string, long> planned { get; set; } = new();
        public long? result { get; set; }
        public long? cashAfterPurchases { get; set; }
        public long? purchasesCut { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        [JsonIgnore]
        public bool IsRecommended { get; set; }

        public string ResultText => $"Résultat {Money(result)}";

        public string CashText => cashAfterPurchases is null ? string.Empty : $"Trésorerie après achats {Money(cashAfterPurchases)}";
        public bool HasCash => cashAfterPurchases.HasValue;

        public string PlannedText => string.Join(" · ", new[]
        {
            ("purchases", "Stock"), ("trip", "Voyage"), ("shipping", "Port"), ("advertising", "Publicité"), ("other", "Autres"),
        }.Where(entry => planned.ContainsKey(entry.Item1))
         .Select(entry => $"{entry.Item2} {Money(planned[entry.Item1])}"));

        public string Money(long? cents) => cents is { } value ? Utilities.SiteCurrency.FormatCents(value, null, Currency) : "—";
    }
}
