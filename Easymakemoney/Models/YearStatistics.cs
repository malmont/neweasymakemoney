using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>
    /// L'exercice de l'entreprise (GET gestion/statistiques/annee, lots backend AN + EX, docs/annee-gestion.md) : mois par mois, résultat de
    /// l'entreprise (marge + locations et abonnements − frais variables − frais fixes), projection, point mort, écart et leviers chiffrés.
    /// </summary>
    public class YearStatistics
    {
        public string? currency { get; set; }
        public int year { get; set; }
        public string? status { get; set; }
        public string? today { get; set; }
        public FiscalYearInfo? fiscalYear { get; set; }
        public List<YearMonth> months { get; set; } = new();
        public List<YearMonth> cumulative { get; set; } = new();
        public YearLastYear? lastYear { get; set; }
        public List<YearPeriod> periods { get; set; } = new();
        public YearMonth? totals { get; set; }
        public YearProfitability? profitability { get; set; }
        public List<YearLever>? levers { get; set; }
        public YearFinished? finished { get; set; }
        public List<StatsCaveat> caveats { get; set; } = new();

        public bool IsFinished => status == "finished";
        public string Label => fiscalYear?.label ?? year.ToString(CultureInfo.InvariantCulture);
        public string StatusText => IsFinished ? "Exercice terminé" : "Exercice en cours";

        public string Money(long? cents) => cents is { } value ? SiteCurrency.FormatCents(value, null, currency) : "—";

        /// <summary>« Du 01/01/2026 au 11/11/2026 » : la partie écoulée de l'exercice.</summary>
        public string ElapsedText => totals == null
            ? string.Empty
            : $"Du {TreasuryDates.Short(totals.from ?? fiscalYear?.from)} au {TreasuryDates.Short(totals.to ?? today)}";

        public long? Result => IsFinished ? finished?.bilan?.result ?? totals?.result : totals?.result;
        public string ResultText => Money(Result);
        public bool IsLoss => Result < 0;

        // Projection de fin d'exercice (exercice en cours)
        public YearMonth? Projection => profitability?.projection;
        public bool HasProjection => !IsFinished && Projection?.result != null && Projection.method != "none";
        public string ProjectionText => HasProjection ? $"Fin d'exercice estimée : {Money(Projection!.result)}" : string.Empty;
        public string ProjectionMethodText => Projection?.method switch
        {
            "lastYear" => "Estimation : le reste de l'exercice comme les mêmes mois de l'exercice précédent, frais selon vos budgets.",
            "pace" => "Estimation : le rythme de vente des 30 derniers jours prolongé jusqu'à la fin de l'exercice, frais selon vos budgets.",
            _ => "Pas assez de ventes pour estimer la fin de l'exercice.",
        };

        // Point mort et écart
        public string TauxMarqueText => profitability?.tauxMarque is { } rate ? rate.ToString("0.#", CultureInfo.CurrentCulture) + " %" : "—";

        public string BreakEvenText => profitability?.breakEven?.sales is { } sales
            ? (profitability.breakEven.reached == true
                ? $"Point mort : {Money(sales)} de ventes sur l'exercice. Atteint."
                : $"Point mort : {Money(sales)} de ventes sur l'exercice. Pas encore atteint.")
            : "Point mort : impossible à calculer sans marge positive.";

        public bool HasGap => profitability?.gap?.margin is > 0;

        public string GapText
        {
            get
            {
                if (profitability?.gap is not { margin: > 0 } gap)
                    return IsFinished
                        ? (Result >= 0 ? "L'exercice a été rentable." : string.Empty)
                        : (HasProjection ? "À ce rythme, l'exercice sera rentable." : string.Empty);
                var text = $"Il manque {Money(gap.margin)} de marge, soit {Money(gap.sales)} de ventes au taux de marque actuel.";
                if (!IsFinished && gap.perRemainingMonth is { margin: > 0 } perMonth)
                    text += $" C'est {Money(perMonth.sales)} de ventes en plus par mois restant.";
                return text;
            }
        }

        public bool HasGapText => GapText.Length > 0;

        /// <summary>Le détail du résultat de l'exercice écoulé (ou de l'exercice entier s'il est terminé), ligne par ligne.</summary>
        public List<YearRow> CompositionRows()
        {
            var source = IsFinished ? finished?.bilan ?? totals : totals;
            if (source == null)
                return new List<YearRow>();
            var rows = new List<YearRow>
            {
                new("Ventes", "ventes", Money(source.sales)),
                new("Marge", "marge", Money(source.grossMargin)),
                new("Locations et abonnements", "autres-revenus", Plus(source.otherRevenue?.total)),
                new("Frais variables", "frais-variables", Minus(source.variableCosts?.total)),
                new("Frais fixes", "frais-fixes", Minus(source.fixedCosts)),
                new("Résultat de l'entreprise", "resultat-entreprise", Money(source.result), IsTotal: true),
            };
            return rows;
        }

        public string VariableCostsDetail
        {
            get
            {
                var costs = (IsFinished ? finished?.bilan ?? totals : totals)?.variableCosts;
                if (costs == null)
                    return string.Empty;
                return $"dont port {Money(costs.shipping)}, publicité {Money(costs.advertising)}, notes de frais {Money(costs.notes)}, voyages {Money(costs.trip)}";
            }
        }

        /// <summary>Points du graphique mois par mois : réalisé, puis estimé (projection) pour les mois à venir.</summary>
        public List<StatsChartPoint> ChartPoints(string metric)
        {
            var points = new List<StatsChartPoint>();
            var projected = Projection?.months ?? new List<YearMonth>();
            var series = metric == "cumulative" ? cumulative : months;
            var lastSeries = metric == "cumulative" ? lastYear?.cumulative : lastYear?.months;
            for (var i = 0; i < series.Count; i++)
            {
                var month = series[i];
                var isFuture = month.elapsed is "false" or null && Value(month, metric) == null;
                var estimate = isFuture && metric != "cumulative"
                    ? projected.FirstOrDefault(p => p.from == month.from || (p.month == month.month && p.year == month.year))
                    : null;
                var value = Value(estimate ?? month, metric);
                var last = lastSeries != null && i < lastSeries.Count ? Value(lastSeries[i], metric) : null;
                points.Add(new StatsChartPoint(
                    month.from ?? i.ToString(CultureInfo.InvariantCulture),
                    MonthLabel(month) + (estimate != null ? " (estimé)" : string.Empty),
                    MonthShort(month),
                    value ?? 0,
                    last,
                    IsEstimate: estimate != null));
            }
            return points;
        }

        private static long? Value(YearMonth month, string metric) => metric switch
        {
            "sales" => month.sales,
            "grossMargin" => month.grossMargin,
            _ => month.result,
        };

        public static string MonthLabel(YearMonth month) =>
            TreasuryDates.Parse(month.from) is { } from ? from.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("fr-CA")) : string.Empty;

        public static string MonthShort(YearMonth month) =>
            TreasuryDates.Parse(month.from) is { } from ? from.ToString("MMM", CultureInfo.GetCultureInfo("fr-CA")) : string.Empty;

        public List<YearPeriodRow> PeriodRows() => periods.Select(p => new YearPeriodRow(p, currency)).ToList();

        public List<YearLeverRow> LeverRows() => (levers ?? new()).Select(l => new YearLeverRow(l, currency)).ToList();

        public string CaveatsText => string.Join("\n", caveats.Where(c => !string.IsNullOrWhiteSpace(c.text)).Select(c => $"• {c.text}"));
        public bool HasCaveats => CaveatsText.Length > 0;

        // Exercice terminé
        public string FinishedSummary
        {
            get
            {
                if (finished?.bilan is not { } bilan)
                    return string.Empty;
                return bilan.profitable == true
                    ? $"Exercice rentable : {Money(bilan.result)} de résultat."
                    : $"Exercice en perte : {Money(bilan.result)}.";
            }
        }

        public string BestMonthsText => MonthsText(finished?.bestMonths);
        public string WorstMonthsText => MonthsText(finished?.worstMonths);

        private string MonthsText(List<YearMonth>? list) => list == null
            ? string.Empty
            : string.Join(" · ", list.Select(m => $"{MonthLabel(m)} {Money(m.result)}"));

        public string HindsightText
        {
            get
            {
                if (finished?.hindsight is not { } h || finished.bilan?.profitable == true)
                    return string.Empty;
                var lines = new List<string>();
                if (h.priceRequiredPercent is { } percent)
                    lines.Add($"• des prix {percent.ToString("0.#", CultureInfo.CurrentCulture)} % plus hauts, à quantités égales ;");
                if (h.fixedCostsCutRequired is { } cut)
                    lines.Add($"• ou {Money(cut)} de frais fixes en moins ;");
                if (h.salesRequired is { } sales)
                    lines.Add($"• ou {Money(sales)} de ventes sur l'exercice (le point mort).");
                return lines.Count == 0 ? string.Empty : "Ce qui aurait suffi pour ne pas perdre d'argent :\n" + string.Join("\n", lines);
            }
        }

        public bool HasHindsight => HindsightText.Length > 0;

        private string Plus(long? cents) => cents is { } value ? $"+ {Money(value)}" : "—";
        private string Minus(long? cents) => cents is { } value ? $"− {Money(value)}" : "—";
    }

    public class FiscalYearInfo
    {
        public int startMonth { get; set; } = 1;
        public int? startYear { get; set; }
        public string? from { get; set; }
        public string? to { get; set; }
        public string? label { get; set; }
    }

    public class YearMonth
    {
        public int month { get; set; }
        public int year { get; set; }
        public string? from { get; set; }
        public string? to { get; set; }

        /// <summary>true, « partial » ou false : lu tel quel (le backend rend un booléen ou un texte).</summary>
        [JsonConverter(typeof(LooseStringConverter))]
        public string? elapsed { get; set; }

        public long? sales { get; set; }
        public long? grossMargin { get; set; }
        public double? grossMarginRate { get; set; }
        /// <summary>Un objet détaillé, ou un simple total (cumuls) : les deux sont lus.</summary>
        [JsonConverter(typeof(TotalOrObjectConverter<YearOtherRevenue>))]
        public YearOtherRevenue? otherRevenue { get; set; }

        [JsonConverter(typeof(TotalOrObjectConverter<YearVariableCosts>))]
        public YearVariableCosts? variableCosts { get; set; }

        public long? fixedCosts { get; set; }
        public long? resultBeforeOtherRevenue { get; set; }
        public long? result { get; set; }

        // Projection
        public string? method { get; set; }
        public string? reason { get; set; }
        public List<YearMonth>? months { get; set; }
        public YearSources? sources { get; set; }

        // Bilan
        public bool? profitable { get; set; }
    }

    public interface IHasTotal
    {
        long? total { get; set; }
    }

    /// <summary>Lit { …, total } ou un nombre seul (pris comme total).</summary>
    public sealed class TotalOrObjectConverter<T> : JsonConverter where T : class, IHasTotal, new()
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(T);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var token = Newtonsoft.Json.Linq.JToken.Load(reader);
            return token.Type switch
            {
                Newtonsoft.Json.Linq.JTokenType.Null => null,
                Newtonsoft.Json.Linq.JTokenType.Integer or Newtonsoft.Json.Linq.JTokenType.Float => new T { total = token.ToObject<long>() },
                Newtonsoft.Json.Linq.JTokenType.Object => token.ToObject<T>(),
                _ => null,
            };
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) => throw new NotSupportedException();
    }

    public class YearOtherRevenue : IHasTotal
    {
        public long? rentals { get; set; }
        public long? subscriptions { get; set; }
        public long? total { get; set; }
    }

    public class YearVariableCosts : IHasTotal
    {
        public long? shipping { get; set; }
        public long? advertising { get; set; }
        public long? notes { get; set; }
        public long? trip { get; set; }
        public long? total { get; set; }
    }

    public class YearSources
    {
        public string? sales { get; set; }
        public string? variableCosts { get; set; }
    }

    public class YearLastYear
    {
        public int year { get; set; }
        public string? label { get; set; }
        public string? from { get; set; }
        public string? to { get; set; }
        public List<YearMonth> months { get; set; } = new();
        public List<YearMonth> cumulative { get; set; } = new();
    }

    public class YearPeriod
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? start { get; set; }
        public string? end { get; set; }
        public string? status { get; set; }
        public bool? started { get; set; }
        public long? revenue { get; set; }
        public long? grossMargin { get; set; }
        public long? net { get; set; }
        public long? netAfterFixed { get; set; }
        public long? otherRevenue { get; set; }
        public bool frozen { get; set; }
        public bool partial { get; set; }
        public int? overlapDays { get; set; }
        public int? days { get; set; }
    }

    public class YearProfitability
    {
        public double? tauxMarque { get; set; }
        public YearMonth? projection { get; set; }
        public YearBreakEven? breakEven { get; set; }
        public YearGap? gap { get; set; }
    }

    public class YearBreakEven
    {
        public long? sales { get; set; }
        public bool? reached { get; set; }
    }

    public class YearGap
    {
        public long? margin { get; set; }
        public long? sales { get; set; }
        public YearGapPerMonth? perRemainingMonth { get; set; }
    }

    public class YearGapPerMonth
    {
        public long? margin { get; set; }
        public long? sales { get; set; }
        public double? remainingMonths { get; set; }
    }

    public class YearLever
    {
        public string? key { get; set; }
        public string? label { get; set; }
        public string? detail { get; set; }
        public long? effect { get; set; }
        public bool coversGap { get; set; }
        public double? percent { get; set; }
        public double? requiredPercent { get; set; }
        public double? weight { get; set; }
    }

    public class YearFinished
    {
        public YearMonth? bilan { get; set; }
        public List<YearMonth>? bestMonths { get; set; }
        public List<YearMonth>? worstMonths { get; set; }
        public YearPeriod? bestPeriod { get; set; }
        public YearPeriod? worstPeriod { get; set; }
        public YearHindsight? hindsight { get; set; }
    }

    public class YearHindsight
    {
        public double? priceRequiredPercent { get; set; }
        public long? fixedCostsCutRequired { get; set; }
        public long? salesRequired { get; set; }
    }

    public sealed record YearRow(string Title, string Term, string Value, bool IsTotal = false);

    public sealed class YearPeriodRow
    {
        public YearPeriodRow(YearPeriod period, string? currency)
        {
            Name = period.name ?? string.Empty;
            DatesText = $"{TreasuryDates.Short(period.start)} → {TreasuryDates.Short(period.end)}";
            var result = (period.netAfterFixed ?? period.net ?? 0) + (period.otherRevenue ?? 0);
            ResultText = SiteCurrency.FormatCents(result, null, currency);
            IsLoss = result < 0;
            var notes = new List<string>();
            notes.Add(period.netAfterFixed.HasValue ? "après frais fixes, locations et abonnements" : "frais fixes non calculés à la fermeture");
            if (period.partial && period.overlapDays is { } overlap && period.days is { } days)
                notes.Add($"à cheval : {overlap} j sur {days} dans l'exercice");
            if (period.frozen)
                notes.Add("chiffres figés");
            else if (period.started == false)
                notes.Add("pas encore commencée");
            NoteText = string.Join(" · ", notes);
        }

        public string Name { get; }
        public string DatesText { get; }
        public string ResultText { get; }
        public bool IsLoss { get; }
        public string NoteText { get; }
        public Microsoft.Maui.Graphics.Color ResultColor => Microsoft.Maui.Graphics.Color.FromArgb(IsLoss ? "#B91C1C" : "#0F172A");
    }

    public sealed class YearLeverRow
    {
        public YearLeverRow(YearLever lever, string? currency)
        {
            Key = lever.key ?? string.Empty;
            Label = lever.label ?? string.Empty;
            Detail = lever.detail ?? string.Empty;
            CoversGap = lever.coversGap;
            EffectText = lever.effect is { } effect
                ? (Key == "worstPeriod"
                    ? $"{SiteCurrency.FormatCents(effect, null, currency)} sous zéro"
                    : $"+ {SiteCurrency.FormatCents(effect, null, currency)} sur l'exercice")
                : string.Empty;
            RequiredText = Key == "price" && lever.requiredPercent is { } required
                ? $"Hausse qui comblerait l'écart : {required.ToString("0.#", CultureInfo.CurrentCulture)} %"
                : string.Empty;
        }

        public string Key { get; }
        public string Label { get; }
        public string Detail { get; }
        public bool CoversGap { get; }
        public string EffectText { get; }
        public string RequiredText { get; }
        public bool HasRequired => RequiredText.Length > 0;
        public bool IsPrice => Key == "price";
    }

    /// <summary>Avis de l'IA sur un exercice terminé (POST gestion/statistiques/annee/avis).</summary>
    public class YearComment
    {
        public int year { get; set; }
        public YearCommentBody? comment { get; set; }
        public bool cached { get; set; }

        public string LessonsText => comment?.lessons is { Count: > 0 } lessons ? string.Join("\n", lessons.Select(l => $"• {l}")) : string.Empty;
    }

    public class YearCommentBody
    {
        public string? title { get; set; }
        public string? text { get; set; }
        public List<string>? lessons { get; set; }
        public string? textSource { get; set; }
    }

    /// <summary>Exercice du site (GET / PUT gestion/reglages/exercice).</summary>
    public class FiscalYearSetting
    {
        public int startMonth { get; set; } = 1;
        public string? label { get; set; }
        public FiscalYearInfo? current { get; set; }
    }

    /// <summary>Lit un booléen, un texte ou un nombre comme texte (« true », « partial », « false »).</summary>
    public sealed class LooseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(string);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) =>
            reader.TokenType switch
            {
                JsonToken.Null => null,
                JsonToken.Boolean => (bool)reader.Value! ? "true" : "false",
                _ => Convert.ToString(reader.Value, CultureInfo.InvariantCulture),
            };

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) => writer.WriteValue(value as string);
    }
}

namespace Easymakemoney.Models
{
    /// <summary>Textes du réglage d'exercice, sans dépendance à l'écran (testables).</summary>
    public static class FiscalYearSettings
    {
        private static readonly string[] Names =
        {
            "janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre",
        };

        public static IReadOnlyList<TreasuryChoice> Months { get; } = Names
            .Select((name, i) => new TreasuryChoice((i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), char.ToUpperInvariant(name[0]) + name[1..]))
            .ToList();

        /// <summary>« De janvier à décembre », « D'avril à mars ».</summary>
        public static string RangeText(int startMonth)
        {
            var start = Names[(startMonth - 1 + 12) % 12];
            var end = Names[(startMonth + 10) % 12];
            var from = start[0] is 'a' or 'o' ? $"D'{start}" : $"De {start}";
            return $"{from} à {end}";
        }

        /// <summary>Les périodes de la gestion sont des trimestres saisonniers (mars–mai, juin–août, sept.–nov., déc.–févr.).</summary>
        public static string AlignmentText(int startMonth) => startMonth is 3 or 6 or 9 or 12
            ? "Ce mois tombe au début d'une période de vente : l'exercice et les périodes coïncident."
            : "Les périodes de vente sont des trimestres saisonniers (mars–mai, juin–août, septembre–novembre, décembre–février). Avec ce mois, une période sera à cheval sur deux exercices : les chiffres mois par mois restent justes, seule la liste des périodes le signale.";
    }
}
