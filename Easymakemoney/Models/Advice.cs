using System.Globalization;
using Easymakemoney.Utilities;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>Conseils du guide de période (GET gestion/conseils, lot backend G1a, docs/guide-ia-gestion.md).</summary>
    public class AdviceList
    {
        public AdvicePeriod? period { get; set; }
        public List<string> moments { get; set; } = new();
        public string? currency { get; set; }
        public string? headline { get; set; }
        public AdviceAi? ai { get; set; }
        public bool generating { get; set; }
        public int manualRefreshesLeft { get; set; }
        public List<Advice> advices { get; set; } = new();

        public AdviceList WithCurrency()
        {
            foreach (var advice in advices)
                advice.Currency = currency;
            return this;
        }

        public bool HasHeadline => !string.IsNullOrWhiteSpace(headline);

        public string MomentsText => moments.Count == 0
            ? string.Empty
            : string.Join(" · ", moments.Select(Advice.MomentLabel));

        public string AiText => generating || ai?.status == "pending"
            ? "Le guide rédige ses conseils… Revenez dans un instant."
            : ai?.status switch
            {
                "ok" => "Conseils rédigés par l'IA à partir de vos chiffres.",
                "credits_exhausted" => "Crédits IA épuisés : conseils calculés sans rédaction par l'IA.",
                "not_included" => "Conseils calculés sur vos chiffres (rédaction par l'IA non incluse dans votre offre).",
                "error" => "Rédaction par l'IA indisponible : conseils calculés sur vos chiffres.",
                _ => "Conseils calculés sur vos chiffres.",
            };

        public bool CanRefresh => !generating && manualRefreshesLeft > 0;
        public string RefreshText => manualRefreshesLeft > 0 ? $"Actualiser ({manualRefreshesLeft} restantes aujourd'hui)" : "Plus d'actualisation aujourd'hui";
    }

    public class AdvicePeriod
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? start { get; set; }
        public string? end { get; set; }
    }

    public class AdviceAi
    {
        public string? status { get; set; }
        public string? generatedAt { get; set; }
    }

    public class Advice
    {
        public int id { get; set; }
        public int periodId { get; set; }
        public string? moment { get; set; }
        public string? kind { get; set; }
        public string? title { get; set; }
        public string? text { get; set; }
        public string? textSource { get; set; }
        public AdviceAction? action { get; set; }
        public AdviceEffect? effect { get; set; }
        public string? confidence { get; set; }
        public List<AdviceSource> sources { get; set; } = new();
        public JToken? figures { get; set; }
        public string? status { get; set; }
        public int rank { get; set; }

        /// <summary>Ce que le gérant a appliqué (G1b-1) et sa date.</summary>
        public JToken? applied { get; set; }
        public string? appliedOn { get; set; }

        /// <summary>Mesure après application, nulle si le conseil n'est pas fait ou pas mesurable.</summary>
        public AdviceMeasurement? measurement { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        /// <summary>Faux : le conseil n'est plus détecté (il reste dans l'historique).</summary>
        public bool active { get; set; } = true;

        public bool IsNew => status == "new" && active;
        public bool IsDone => status == "done";

        public string AppliedText => appliedOn != null ? $"Appliqué le {TreasuryDates.Short(appliedOn[..Math.Min(10, appliedOn.Length)])}" : "Appliqué";

        public string MeasurementText => measurement?.text is { Length: > 0 } text
            ? text
            : measurement == null ? "Pas de mesure chiffrée pour ce conseil." : "Mesure en cours…";

        public bool HasMeasuredGain => measurement?.complete == true;

        /// <summary>
        /// Ce que le gérant peut préciser au « C'est fait » : hausse de prix ou promotion, préremplie avec le pourcentage conseillé.
        /// Nul : le conseil n'a pas de pourcentage à préciser.
        /// </summary>
        public string? AppliedPercentField => kind switch
        {
            "FAST_SELLER" => "pricePercent",
            "SLOW_STOCK" or "END_CLEARANCE" => "promotionPercent",
            _ => null,
        };

        /// <summary>Hausse conseillée sur ce qui part vite : le gérant peut l'appliquer directement (lot P4).</summary>
        public bool CanChangePrice => IsNew && kind == "FAST_SELLER";

        /// <summary>Promotion conseillée sur ce qui dort ou en fin de période : le gérant peut la créer directement (lot P5).</summary>
        public bool CanCreatePromotion => IsNew && kind is "SLOW_STOCK" or "END_CLEARANCE";

        public double? SuggestedPercent =>
            sources.FirstOrDefault(source => source.unit == "percent" && source.value?.Type is JTokenType.Integer or JTokenType.Float)?.value?.Value<double>();

        public string MomentText => MomentLabel(moment);

        public static string MomentLabel(string? moment) => moment switch
        {
            "trip" => "Voyage d'achat",
            "selling" => "Pendant la vente",
            "end" => "Fin de période",
            "closing" => "Bilan",
            "planning" => "Préparation",
            _ => moment ?? string.Empty,
        };

        public string ConfidenceText => confidence switch
        {
            "high" => "Confiance élevée",
            "medium" => "Confiance moyenne",
            _ => "Estimation prudente",
        };

        /// <summary>Effet signé du point de vue du gérant : positif = gagné ou libéré, négatif = coût ou risque.</summary>
        public string EffectText => effect == null
            ? string.Empty
            : $"{effect.label} : {SiteCurrency.FormatCents(effect.amount, null, Currency)}";

        public bool HasEffect => effect != null;
        public bool IsRisk => effect?.amount < 0;

        public string SourcesText => string.Join("\n", sources.Select(source => $"{source.label} : {source.Format(Currency)}"));
        public bool HasSources => sources.Count > 0;

        public bool HasAction => !string.IsNullOrWhiteSpace(action?.screen);

        public string ActionText => action?.screen switch
        {
            "simulateur" => "Essayer dans le simulateur",
            "articles" => "Voir les articles",
            "campagnes.new" => "Relancer la campagne",
            "periode.budget" => "Voir le budget",
            "voyages.suivi" => "Voir le voyage",
            "tresorerie" => "Voir la trésorerie",
            "documents.scan" => "Lire un document",
            _ => "Ouvrir",
        };

        public bool WrittenByAi => textSource == "ai";
    }

    public class AdviceAction
    {
        public string? screen { get; set; }

        [JsonProperty("params")]
        public JToken? @params { get; set; }
    }

    public class AdviceMeasurement
    {
        public bool complete { get; set; }
        public AdviceChange? change { get; set; }
        public string? text { get; set; }
    }

    public class AdviceChange
    {
        public long? grossMargin { get; set; }
        public long? revenue { get; set; }
    }

    public class AdviceEffect
    {
        public long amount { get; set; }
        public string? label { get; set; }
    }

    public class AdviceSource
    {
        public string? label { get; set; }
        public JToken? value { get; set; }
        public string? unit { get; set; }

        /// <summary>Met en forme la valeur selon son unité (money en centimes, percent, count, days, date, ratio).</summary>
        public string Format(string? currency)
        {
            if (value == null || value.Type == JTokenType.Null)
                return "—";

            var culture = CultureInfo.CurrentCulture;
            switch (unit)
            {
                case "money" when value.Type is JTokenType.Integer or JTokenType.Float:
                    return SiteCurrency.FormatCents(value.Value<double>(), null, currency);
                case "percent" when value.Type is JTokenType.Integer or JTokenType.Float:
                    return value.Value<double>().ToString("0.##", culture) + " %";
                case "days" when value.Type is JTokenType.Integer or JTokenType.Float:
                    var days = value.Value<double>();
                    return days == 1 ? "1 jour" : days.ToString("0.#", culture) + " jours";
                case "ratio" when value.Type is JTokenType.Integer or JTokenType.Float:
                    return value.Value<double>().ToString("0.##", culture);
                case "count" when value.Type is JTokenType.Integer or JTokenType.Float:
                    return value.Value<double>().ToString("0.##", culture);
                case "date":
                    var text = value.Value<string>();
                    return TreasuryDates.Short(text?.Length >= 10 ? text[..10] : text);
                default:
                    return value.ToString();
            }
        }
    }
}
