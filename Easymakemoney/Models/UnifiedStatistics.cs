using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Statistiques unifiées (GET gestion/statistiques, lot backend S1, docs/statistiques-gestion.md) : mêmes règles que le résultat de période,
    /// comparées à la plage précédente et à la même plage l'an dernier.
    /// </summary>
    public class UnifiedStatistics
    {
        public string? currency { get; set; }
        public string? source { get; set; }
        public StatsRange? range { get; set; }
        public StatsDates? previous { get; set; }
        public StatsDates? lastYear { get; set; }
        public StatsComparison? comparison { get; set; }
        public StatsIndicators indicators { get; set; } = new();
        public StatsOtherRevenue? otherRevenue { get; set; }
        public StatsChart? chart { get; set; }
        public List<StatsCaveat> caveats { get; set; } = new();

        /// <summary>Volets demandés par sections=… (lot S2) ; absent quand ils ne sont pas demandés.</summary>
        public StatsSections? sections { get; set; }

        /// <summary>« 1-11 nov. · comparé au 1-11 oct. et au 1-11 nov. 2025 ».</summary>
        public string ComparisonText
        {
            get
            {
                if (comparison == null)
                    return string.Empty;
                var against = new[] { comparison.previousLabel, comparison.lastYearLabel }.Where(label => !string.IsNullOrWhiteSpace(label)).ToList();
                return against.Count == 0
                    ? comparison.currentLabel ?? string.Empty
                    : $"{comparison.currentLabel} · comparé à {string.Join(" et à ", against)}";
            }
        }

        public bool HasPrevious => previous != null;

        /// <summary>Cartes dans l'ordre de lecture ; la valeur du stock vient en dernier, sans courbe d'historique tant qu'il n'y a pas d'instantané.</summary>
        public List<StatsCard> Cards()
        {
            var cards = new List<StatsCard>();
            void Add(string key, string title, string term, StatsIndicator? indicator, bool? higherIsBetter)
            {
                if (indicator != null)
                    cards.Add(new StatsCard(key, title, term, indicator, higherIsBetter, currency, HasPrevious));
            }

            Add("revenue", "Ventes", "ventes", indicators.revenue, true);
            Add("grossMargin", "Marge", "marge", indicators.grossMargin, true);
            Add("grossMarginRate", "Taux de marque", "taux-de-marque", indicators.grossMarginRate, true);
            Add("orders", "Commandes", "commandes", indicators.orders, true);
            Add("averageBasket", "Panier moyen", "panier-moyen", indicators.averageBasket, true);
            Add("returnRate", "Taux de retour", "taux-retour", indicators.returnRate, false);
            Add("stockValue", "Valeur du stock", "valeur-stock", indicators.stockValue, null);
            return cards;
        }

        public List<StatsOtherRevenueRow> OtherRevenueRows()
        {
            var rows = new List<StatsOtherRevenueRow>();
            if (otherRevenue?.rentals is { } rentals)
                rows.Add(new StatsOtherRevenueRow("Locations", rentals, currency, HasPrevious));
            if (otherRevenue?.subscriptions is { } subscriptions)
                rows.Add(new StatsOtherRevenueRow("Abonnements", subscriptions, currency, HasPrevious));
            return rows.Where(row => row.HasAny).ToList();
        }

        public string CaveatsText => string.Join("\n", caveats.Where(c => !string.IsNullOrWhiteSpace(c.text)).Select(c => $"• {c.text}"));
        public bool HasCaveats => CaveatsText.Length > 0;

        /// <summary>Points du graphique pour l'indicateur choisi : revenue, grossMargin, orders, ou un volet (received : encaissé, expenses : total des frais).</summary>
        public List<StatsChartPoint> ChartPoints(string metric)
        {
            var section = metric switch
            {
                "received" => sections?.payments?.chart,
                "expenses" => sections?.expenses?.chart,
                _ => null,
            };
            if (section != null)
            {
                var field = metric == "received" ? "received" : "total";
                return section.points.Select(point =>
                {
                    var asPoint = new StatsPoint { key = point.key, from = point.from, to = point.to };
                    var value = field == "received" ? point.received : point.total;
                    return new StatsChartPoint(
                        point.key ?? string.Empty,
                        StatsFormat.PointLabel(asPoint, section.granularity),
                        StatsFormat.ShortLabel(asPoint, section.granularity),
                        value ?? 0,
                        point.LastYearValue(field));
                }).ToList();
            }
            if (metric is "received" or "expenses")
                return new List<StatsChartPoint>();
            return MainChartPoints(metric);
        }

        private List<StatsChartPoint> MainChartPoints(string metric) => (chart?.points ?? new())
            .Select(point => new StatsChartPoint(
                point.key ?? string.Empty,
                StatsFormat.PointLabel(point, chart?.granularity),
                StatsFormat.ShortLabel(point, chart?.granularity),
                metric switch { "grossMargin" => point.grossMargin, "orders" => point.orders, _ => point.revenue },
                point.lastYear == null ? null : metric switch { "grossMargin" => point.lastYear.grossMargin, "orders" => point.lastYear.orders, _ => point.lastYear.revenue }))
            .ToList();
    }

    public class StatsRange
    {
        public string? type { get; set; }
        public string? from { get; set; }
        public string? to { get; set; }
        public int days { get; set; }
        public string? granularity { get; set; }
    }

    public class StatsDates
    {
        public string? from { get; set; }
        public string? to { get; set; }
    }

    public class StatsComparison
    {
        public string? currentLabel { get; set; }
        public string? previousLabel { get; set; }
        public string? lastYearLabel { get; set; }
    }

    public class StatsIndicators
    {
        public StatsIndicator? revenue { get; set; }
        public StatsIndicator? grossMargin { get; set; }
        public StatsIndicator? grossMarginRate { get; set; }
        public StatsIndicator? orders { get; set; }
        public StatsIndicator? averageBasket { get; set; }
        public StatsIndicator? returnRate { get; set; }
        public StatsIndicator? stockValue { get; set; }
    }

    public class StatsIndicator
    {
        public double? value { get; set; }
        public string? unit { get; set; }
        public double? previous { get; set; }
        public double? lastYear { get; set; }
        public double? changeVsPrevious { get; set; }
        public double? changeVsLastYear { get; set; }
        public string? changeUnit { get; set; }
        public List<double?>? spark { get; set; }

        /// <summary>Valeur du stock : valeur au coût connu.</summary>
        public double? costValue { get; set; }

        /// <summary>Taux de retour : montant et nombre de ventes retournées.</summary>
        public StatsReturns? returns { get; set; }
    }

    public class StatsReturns
    {
        public long amount { get; set; }
        public int orders { get; set; }
    }

    public class StatsOtherRevenue
    {
        public StatsIndicator? rentals { get; set; }
        public StatsIndicator? subscriptions { get; set; }
    }

    public class StatsChart
    {
        public string? granularity { get; set; }
        public List<StatsPoint> points { get; set; } = new();
    }

    public class StatsPoint
    {
        public string? key { get; set; }
        public string? from { get; set; }
        public string? to { get; set; }
        public double revenue { get; set; }
        public double grossMargin { get; set; }
        public double orders { get; set; }
        public StatsPointValues? lastYear { get; set; }
    }

    public class StatsPointValues
    {
        public double revenue { get; set; }
        public double grossMargin { get; set; }
        public double orders { get; set; }
    }

    public class StatsCaveat
    {
        public string? code { get; set; }
        public string? text { get; set; }
        public long? amount { get; set; }
    }

    /// <summary>Une carte d'indicateur prête à afficher.</summary>
    public sealed class StatsCard
    {
        public StatsCard(string key, string title, string term, StatsIndicator indicator, bool? higherIsBetter, string? currency, bool hasPrevious)
        {
            Key = key;
            Title = title;
            Term = term;
            Indicator = indicator;
            ValueText = StatsFormat.Value(indicator.value, indicator.unit, currency);
            PreviousText = hasPrevious ? StatsFormat.Change(indicator.changeVsPrevious, indicator.changeUnit, "vs précédent") : string.Empty;
            LastYearText = StatsFormat.Change(indicator.changeVsLastYear, indicator.changeUnit, "vs an dernier");
            PreviousTone = StatsFormat.Tone(indicator.changeVsPrevious, higherIsBetter);
            LastYearTone = StatsFormat.Tone(indicator.changeVsLastYear, higherIsBetter);
            Spark = indicator.spark?.Select(v => v ?? 0).ToList() ?? new List<double>();
            Detail = key switch
            {
                "stockValue" when indicator.costValue is { } cost => $"{StatsFormat.Value(cost, "money", currency)} au coût",
                "returnRate" when indicator.returns is { orders: > 0 } r => $"{StatsFormat.Value(r.amount, "money", currency)} sur {r.orders} vente(s)",
                _ => string.Empty,
            };
        }

        public string Key { get; }
        public string Title { get; }
        public string Term { get; }
        public StatsIndicator Indicator { get; }
        public string ValueText { get; }
        public string PreviousText { get; }
        public string LastYearText { get; }

        /// <summary>1 : bonne évolution (vert), −1 : mauvaise (rouge), 0 : neutre.</summary>
        public int PreviousTone { get; }
        public int LastYearTone { get; }
        public List<double> Spark { get; }
        /// <summary>Pas de courbe plate : il faut au moins deux valeurs différentes.</summary>
        public bool HasSpark => Spark.Count > 1 && Spark.Distinct().Count() > 1;
        public string Detail { get; }
        public bool HasDetail => Detail.Length > 0;
        public bool HasPreviousText => PreviousText.Length > 0;
        public bool HasLastYearText => LastYearText.Length > 0;
        public Microsoft.Maui.Graphics.Color PreviousColor => StatsFormat.ToneColor(PreviousTone);
        public Microsoft.Maui.Graphics.Color LastYearColor => StatsFormat.ToneColor(LastYearTone);
    }

    /// <summary>Deux cartes côte à côte.</summary>
    public sealed record StatsCardRow(StatsCard Left, StatsCard? Right)
    {
        public bool HasRight => Right != null;

        public static List<StatsCardRow> Pair(IReadOnlyList<StatsCard> cards) => Enumerable
            .Range(0, (cards.Count + 1) / 2)
            .Select(i => new StatsCardRow(cards[2 * i], 2 * i + 1 < cards.Count ? cards[2 * i + 1] : null))
            .ToList();
    }

    public sealed class StatsOtherRevenueRow
    {
        public StatsOtherRevenueRow(string title, StatsIndicator indicator, string? currency, bool hasPrevious)
        {
            Title = title;
            HasAny = (indicator.value ?? 0) != 0 || (indicator.previous ?? 0) != 0 || (indicator.lastYear ?? 0) != 0;
            ValueText = StatsFormat.Value(indicator.value, indicator.unit ?? "money", currency);
            var changes = new[]
            {
                hasPrevious ? StatsFormat.Change(indicator.changeVsPrevious, indicator.changeUnit, "vs précédent") : string.Empty,
                StatsFormat.Change(indicator.changeVsLastYear, indicator.changeUnit, "vs an dernier"),
            };
            ChangeText = string.Join(" · ", changes.Where(text => text.Length > 0));
        }

        public string Title { get; }
        public bool HasAny { get; }
        public string ValueText { get; }
        public string ChangeText { get; }
    }

    public sealed record StatsChartPoint(string Key, string Label, string ShortLabel, double Value, double? LastYear, bool IsEstimate = false);

    public static class StatsFormat
    {
        public static string Value(double? value, string? unit, string? currency)
        {
            if (value is not { } v)
                return "—";
            return unit switch
            {
                "money" => SiteCurrency.FormatCents(v, null, currency),
                "percent" => v.ToString("0.#", CultureInfo.CurrentCulture) + " %",
                _ => Math.Round(v).ToString("N0", CultureInfo.CurrentCulture),
            };
        }

        /// <summary>« +12,3 % vs an dernier » ; « −1,5 pt vs précédent » pour un taux.</summary>
        public static string Change(double? change, string? changeUnit, string suffix)
        {
            if (change is not { } c)
                return string.Empty;
            var sign = c > 0 ? "+" : c < 0 ? "−" : string.Empty;
            var amount = Math.Abs(c).ToString("0.#", CultureInfo.CurrentCulture);
            var unit = changeUnit == "points" ? " pt" : " %";
            return $"{sign}{amount}{unit} {suffix}";
        }

        public static int Tone(double? change, bool? higherIsBetter)
        {
            if (change is not { } c || c == 0 || higherIsBetter is null)
                return 0;
            return (c > 0) == higherIsBetter ? 1 : -1;
        }

        public static Microsoft.Maui.Graphics.Color ToneColor(int tone) => Microsoft.Maui.Graphics.Color.FromArgb(tone switch
        {
            1 => "#047857",
            -1 => "#B91C1C",
            _ => "#64748B",
        });

        /// <summary>Libellé d'axe : « 3 nov. » pour un jour ou une semaine, « nov. » pour un mois.</summary>
        public static string ShortLabel(StatsPoint point, string? granularity)
        {
            var culture = CultureInfo.GetCultureInfo("fr-CA");
            var from = TreasuryDates.Parse(point.from);
            if (from == null)
                return point.key ?? string.Empty;
            return granularity == "month" ? from.Value.ToString("MMM", culture) : from.Value.ToString("d MMM", culture);
        }

        /// <summary>Libellé court d'un point : « 3 nov. » (jour), « sem. du 3 nov. » (semaine), « nov. 2026 » (mois).</summary>
        public static string PointLabel(StatsPoint point, string? granularity)
        {
            var culture = CultureInfo.GetCultureInfo("fr-CA");
            var from = TreasuryDates.Parse(point.from);
            if (from == null)
                return point.key ?? string.Empty;
            return granularity switch
            {
                "month" => from.Value.ToString("MMM yyyy", culture),
                "week" => $"sem. du {from.Value.ToString("d MMM", culture)}",
                _ => from.Value.ToString("ddd d MMM", culture),
            };
        }
    }

    /// <summary>Plage demandée : un raccourci (today, 7d, month, year), une période de vente ou des dates libres.</summary>
    public sealed record StatsQuery(string? Range, int? PeriodId = null, DateTime? From = null, DateTime? To = null, string Source = "all", string? Sections = null)
    {
        public string Url(string locale)
        {
            var parts = new List<string>();
            if (PeriodId is { } id)
                parts.Add($"periodId={id}");
            else if (From is { } from && To is { } to)
            {
                parts.Add($"from={TreasuryDates.Iso(from)}");
                parts.Add($"to={TreasuryDates.Iso(to)}");
            }
            else
                parts.Add($"range={Range ?? "month"}");
            parts.Add($"source={Source}");
            parts.Add($"locale={locale}");
            if (!string.IsNullOrWhiteSpace(Sections))
                parts.Add($"sections={Sections}");
            return "gestion/statistiques?" + string.Join("&", parts);
        }
    }
}
