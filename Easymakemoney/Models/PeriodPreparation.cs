using System.Globalization;
using Easymakemoney.Utilities;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>Lit un montant en cents, envoyé seul ou dans un objet { amount | revenue | saleValue } ; null s'il manque.</summary>
    public static class PreparationValues
    {
        public static long? Cents(JToken? token, params string[] keys)
        {
            switch (token)
            {
                case null:
                    return null;
                case JValue value when value.Type is JTokenType.Integer or JTokenType.Float:
                    return value.Value<long>();
                case JObject obj:
                    foreach (var key in keys.Length > 0 ? keys : new[] { "amount", "revenue", "saleValue" })
                        if (obj[key] is JValue inner && inner.Type is JTokenType.Integer or JTokenType.Float)
                            return inner.Value<long>();
                    return null;
                default:
                    return null;
            }
        }

        public static string Money(long? cents, string? currency) =>
            cents is { } value ? SiteCurrency.FormatCents(value, null, currency) : "—";
    }

    /// <summary>Estimations des ventes d'une période (GET gestion/preparation/estimations, lot backend P6).</summary>
    public class PreparationEstimates
    {
        public AdvicePeriod? period { get; set; }
        public int days { get; set; }
        public JToken? lastYear { get; set; }
        public JToken? average { get; set; }
        public JToken? pace { get; set; }
        public PreparationRecommended? recommended { get; set; }

        public long? LastYearRevenue => PreparationValues.Cents(lastYear, "revenue");
        public long? AverageRevenue => PreparationValues.Cents(average, "revenue");
        public long? PaceRevenue => PreparationValues.Cents(pace, "revenue");

        public long? RevenueFor(string method) => method switch
        {
            "lastYear" => LastYearRevenue,
            "average" => AverageRevenue,
            "pace" => PaceRevenue,
            _ => null,
        };
    }

    public class PreparationRecommended
    {
        public string? method { get; set; }
        public string? reason { get; set; }
    }

    /// <summary>Plan simulé d'une période (POST gestion/preparation/simulation). Rien n'est écrit.</summary>
    public class PreparationSimulation
    {
        public AdvicePeriod? period { get; set; }
        public string? currency { get; set; }
        public PreparationPlan plan { get; set; } = new();
        public JToken? lastYear { get; set; }
        public List<PreparationPlanned> planned { get; set; } = new();
        public string? previewHash { get; set; }
        public List<TreasuryCaveat> caveats { get; set; } = new();

        public string Money(long? cents) => PreparationValues.Money(cents, currency);

        /// <summary>Lignes du résultat, avec leur définition (titres touchables comme dans le simulateur).</summary>
        public List<SimulationRow> Rows()
        {
            var rows = new List<SimulationRow>
            {
                Row("Ventes prévues", PreparationValues.Cents(plan.sales, "planned", "target"),
                    "Les ventes que vous visez pour la période, plafonnées au stock disponible (stock actuel + achats prévus × coefficient)."),
                Row("Marge brute", PreparationValues.Cents(plan.grossMargin),
                    "Ventes moins le coût des articles vendus. La part vendue sur les achats prévus prend la marge de leur coefficient ; la part vendue sur le stock actuel prend sa marge réelle."),
                new SimulationRow
                {
                    Label = "Taux de marque",
                    BaseText = plan.tauxMarque is { } rate ? rate.ToString("0.##", CultureInfo.CurrentCulture) + " %" : "—",
                    Help = "La part de marge dans chaque vente : à 40 %, sur 100 $ vendus, il vous reste 40 $ après le coût d'achat.",
                },
                Row("Frais de la période", PreparationValues.Cents(plan.expenses, "total"),
                    "Voyage, frais de port, publicité et autres frais prévus pour la période. Un voyage fait dans une période précédente compte dans cette période-là."),
                Row("Résultat", PreparationValues.Cents(plan.result),
                    "Marge brute moins frais : ce que la période vous rapportera si le plan se réalise. Les achats de stock ne sont pas une charge : ils deviennent du stock."),
                Row("Point mort (ventes nécessaires)", PreparationValues.Cents(plan.breakEven, "revenue"),
                    "Les ventes à atteindre pour ne pas perdre d'argent sur la période : frais ÷ taux de marque."),
                Row("Achats prévus", PreparationValues.Cents(plan.purchases, "total"),
                    "Le total des achats de stock que vous prévoyez, voyage compris. Ils deviennent le prévu « Stock » du budget de la période de leur date."),
                Row("Stock en fin de période", PreparationValues.Cents(plan.stockEnd, "saleValue"),
                    "La valeur de vente du stock qu'il vous restera à la fin de la période, si le plan se réalise."),
                Row("Point bas de trésorerie", PreparationValues.Cents(plan.cash?["low"], "amount"),
                    "Le solde le plus bas de votre compte d'ici la fin de la période, avec les achats et le voyage à leur date. Un tiret : aucun solde n'est saisi dans Trésorerie."),
            };
            return rows;
        }

        public string CashLowDateText => plan.cash?["low"]?["date"]?.Value<string>() is { } date ? $"le {TreasuryDates.Short(date)}" : string.Empty;

        public string LastYearText => PreparationValues.Cents(lastYear, "revenue") is { } revenue
            ? $"L'an dernier, même période : {Money(revenue)} de ventes, résultat {Money(PreparationValues.Cents(lastYear, "result"))}."
            : string.Empty;

        public bool HasLastYear => LastYearText.Length > 0;

        /// <summary>Ce que « Valider » écrira, période par période.</summary>
        public string PlannedText => string.Join("\n", planned.Select(p =>
            $"• {p.period?.name} ({(p.mode == "add" ? "ajouté au budget existant" : "remplace le budget")}) : stock {Money(p.purchases)}, voyage {Money(p.trip)}, port {Money(p.shipping)}, publicité {Money(p.advertising)}, autres {Money(p.other)}"));

        public string CaveatsText => string.Join("\n", caveats.Where(c => !string.IsNullOrWhiteSpace(c.text)).Select(c => $"• {c.text}"));
        public bool HasCaveats => CaveatsText.Length > 0;

        private SimulationRow Row(string label, long? cents, string help) => new()
        {
            Label = label,
            BaseText = Money(cents),
            Help = help,
            IsTotal = label == "Résultat",
        };
    }

    public class PreparationPlan
    {
        public JToken? sales { get; set; }
        public JToken? grossMargin { get; set; }
        public double? tauxMarque { get; set; }
        public JToken? expenses { get; set; }
        public JToken? result { get; set; }
        public JToken? breakEven { get; set; }
        public JToken? purchases { get; set; }
        public JToken? stockStart { get; set; }
        public JToken? stockEnd { get; set; }
        public JObject? cash { get; set; }

        public bool SalesCapped => sales is JObject s && s.Value<bool?>("capped") == true;
        public bool TripInOtherPeriod => expenses is JObject e && e.Value<bool?>("tripInOtherPeriod") == true;
    }

    public class PreparationPlanned
    {
        public AdvicePeriod? period { get; set; }
        public long? purchases { get; set; }
        public long? trip { get; set; }
        public long? shipping { get; set; }
        public long? advertising { get; set; }
        public long? other { get; set; }
        public string? mode { get; set; }
    }

    /// <summary>Avis du guide sur le plan (POST gestion/preparation/avis).</summary>
    public class PreparationAdvice
    {
        public PreparationSimulation? simulation { get; set; }
        public List<PreparationCandidate> candidates { get; set; } = new();
        public SimulationCommentText? comment { get; set; }

        public PreparationAdvice Prepare()
        {
            foreach (var candidate in candidates)
            {
                candidate.Currency = simulation?.currency;
                candidate.IsRecommended = candidate.key == comment?.recommended;
            }
            return this;
        }

        public string SourceText => comment?.textSource == "ai" ? "Avis rédigé par l'IA à partir des chiffres ci-dessous." : "Avis calculé sur vos chiffres.";
    }

    public class PreparationCandidate
    {
        public string? key { get; set; }
        public string? label { get; set; }
        public JToken? purchases { get; set; }
        public JToken? sales { get; set; }
        public JToken? result { get; set; }
        public JToken? cashLow { get; set; }

        [JsonIgnore] public string? Currency { get; set; }
        [JsonIgnore] public bool IsRecommended { get; set; }

        public string SummaryText =>
            $"Achats {PreparationValues.Money(PreparationValues.Cents(purchases, "total"), Currency)} · ventes {PreparationValues.Money(PreparationValues.Cents(sales, "planned", "target"), Currency)}";

        public string ResultText => $"Résultat {PreparationValues.Money(PreparationValues.Cents(result), Currency)}";

        public string CashText => PreparationValues.Cents(cashLow, "amount") is { } low
            ? $"Point bas de trésorerie {PreparationValues.Money(low, Currency)}"
            : string.Empty;

        public bool HasCash => CashText.Length > 0;
    }

    /// <summary>Corps commun de la simulation, de l'avis et de la validation.</summary>
    public sealed class PreparationRequest
    {
        public string? Date { get; init; }
        public string SalesMethod { get; init; } = "lastYear";
        public long? SalesAmount { get; init; }
        public List<PreparationPurchase> Purchases { get; init; } = new();
        public PreparationTrip? Trip { get; init; }
        public long? Shipping { get; init; }
        public long? Advertising { get; init; }
        public long? Other { get; init; }

        public Dictionary<string, object> ToBody()
        {
            var sales = new Dictionary<string, object> { ["method"] = SalesMethod };
            if (SalesMethod == "custom" && SalesAmount.HasValue)
                sales["amount"] = SalesAmount.Value;

            var body = new Dictionary<string, object>
            {
                ["date"] = Date ?? TreasuryDates.Iso(DateTime.Today),
                ["sales"] = sales,
                ["purchases"] = Purchases.Select(p =>
                {
                    var line = new Dictionary<string, object> { ["date"] = TreasuryDates.Iso(p.Date), ["amount"] = p.AmountCents };
                    if (p.Coefficient is { } coefficient)
                        line["coefficient"] = coefficient;
                    return line;
                }).ToList(),
                ["locale"] = "fr",
            };

            if (Trip != null)
            {
                var trip = new Dictionary<string, object>
                {
                    ["startDate"] = TreasuryDates.Iso(Trip.StartDate),
                    ["endDate"] = TreasuryDates.Iso(Trip.EndDate),
                    ["budgetTotal"] = Trip.BudgetTotalCents,
                };
                if (!string.IsNullOrWhiteSpace(Trip.Place))
                    trip["place"] = Trip.Place.Trim();
                body["trip"] = trip;
            }

            var planned = new Dictionary<string, object>();
            if (Shipping is > 0) planned["shipping"] = Shipping.Value;
            if (Advertising is > 0) planned["advertising"] = Advertising.Value;
            if (Other is > 0) planned["other"] = Other.Value;
            if (planned.Count > 0)
                body["planned"] = planned;
            return body;
        }
    }

    public sealed class PreparationPurchase
    {
        public DateTime Date { get; init; }
        public long AmountCents { get; init; }
        public double? Coefficient { get; init; }

        public string Text => Coefficient is { } c
            ? $"{TreasuryDates.Short(TreasuryDates.Iso(Date))} · {SiteCurrency.FormatCents(AmountCents)} · coefficient {c.ToString("0.##", CultureInfo.CurrentCulture)}"
            : $"{TreasuryDates.Short(TreasuryDates.Iso(Date))} · {SiteCurrency.FormatCents(AmountCents)} · coefficient du site";
    }

    public sealed class PreparationTrip
    {
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public string? Place { get; init; }
        public long BudgetTotalCents { get; init; }
    }
}
