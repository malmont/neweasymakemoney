using System.Globalization;
using Easymakemoney.Utilities;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Statistiques des locations (GET gestion/locations/statistiques, lot backend LOC1, docs/locations-gestion.md) : revenu à la date de vente
    /// (comme le résultat de période) et par date de location, occupation par article et par jour de la semaine.
    /// </summary>
    public class RentalStatistics
    {
        public string? currency { get; set; }
        public RentalRange? range { get; set; }
        public long revenue { get; set; }
        public long revenueByRentalDate { get; set; }
        public int rentals { get; set; }
        public int cancellations { get; set; }
        public List<RentalProduct> products { get; set; } = new();
        public List<RentalWeekday> weekdays { get; set; } = new();
        public List<JToken> highDays { get; set; } = new();
        public List<JToken> lowDays { get; set; } = new();
        public RentalThresholds? thresholds { get; set; }
        public List<StatsCaveat> caveats { get; set; } = new();

        public string Money(long cents) => SiteCurrency.FormatCents(cents, null, currency);

        public string RangeText => range == null
            ? string.Empty
            : range.type switch
            {
                "period" => $"Période en cours : du {TreasuryDates.Short(range.from)} au {TreasuryDates.Short(range.to)} ({range.days} jours)",
                "last30" => $"30 derniers jours : du {TreasuryDates.Short(range.from)} au {TreasuryDates.Short(range.to)}",
                _ => $"Du {TreasuryDates.Short(range.from)} au {TreasuryDates.Short(range.to)} ({range.days} jours)",
            };

        public string RevenueText => Money(revenue);
        public string RevenueByRentalDateText => Money(revenueByRentalDate);
        public string RentalsText => rentals.ToString("N0", CultureInfo.CurrentCulture);
        public string CancellationsText => cancellations.ToString("N0", CultureInfo.CurrentCulture);

        public bool HasProducts => products.Count > 0;

        public List<RentalProductRow> ProductRows() => products
            .OrderByDescending(p => p.occupancy ?? 0)
            .Select(p => new RentalProductRow(p, currency))
            .ToList();

        /// <summary>Occupation moyenne par jour de la semaine, du lundi au dimanche.</summary>
        public List<StatsChartPoint> WeekdayPoints() => weekdays
            .OrderBy(d => d.weekday)
            .Select(d => new StatsChartPoint(
                d.weekday.ToString(CultureInfo.InvariantCulture),
                $"{d.label} : {RentalFormat.Percent(d.occupancy)} d'occupation, {d.rentals} location(s)",
                ShortDay(d),
                d.occupancy ?? 0,
                null))
            .ToList();

        private static string ShortDay(RentalWeekday day) =>
            string.IsNullOrEmpty(day.label) ? day.weekday.ToString(CultureInfo.InvariantCulture) : day.label.Length > 3 ? day.label[..3] + "." : day.label;

        public string HighDaysText => DaysText(highDays, $"Jours pleins (≥ {thresholds?.peak ?? 80} %)");
        public string LowDaysText => DaysText(lowDays, $"Jours creux (< {thresholds?.low ?? 15} %)");
        public bool HasHighDays => highDays.Count > 0;
        public bool HasLowDays => lowDays.Count > 0;

        /// <summary>Jours pleins ou creux : objets (label, occupation) ou textes, lus tels quels.</summary>
        private string DaysText(List<JToken> days, string title)
        {
            var items = days.Select(day => day switch
            {
                JObject o => o.Value<string>("label") is { } label
                    ? (o["occupancy"] is JValue occ && occ.Type is JTokenType.Integer or JTokenType.Float
                        ? $"{label} ({RentalFormat.Percent(occ.Value<double>())})"
                        : label)
                    : o.Value<string>("date") is { } date ? TreasuryDates.Short(date) : o.ToString(Newtonsoft.Json.Formatting.None),
                JValue { Type: JTokenType.Integer } v => WeekdayName(v.Value<int>()),
                JValue v => v.ToString(CultureInfo.CurrentCulture),
                _ => day.ToString(),
            }).Where(text => !string.IsNullOrWhiteSpace(text)).ToList();
            return items.Count == 0 ? string.Empty : $"{title} : {string.Join(", ", items)}";
        }

        /// <summary>1 lundi … 7 dimanche ; le libellé du backend s'il est fourni.</summary>
        public string WeekdayName(int weekday) =>
            weekdays.FirstOrDefault(d => d.weekday == weekday)?.label is { Length: > 0 } label
                ? label
                : weekday is >= 1 and <= 7
                    ? new[] { "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi", "dimanche" }[weekday - 1]
                    : weekday.ToString(CultureInfo.InvariantCulture);

        public string CaveatsText => string.Join("\n", caveats.Where(c => !string.IsNullOrWhiteSpace(c.text)).Select(c => $"• {c.text}"));
        public bool HasCaveats => CaveatsText.Length > 0;
    }

    public class RentalRange
    {
        public string? type { get; set; }
        public string? from { get; set; }
        public string? to { get; set; }
        public int days { get; set; }
    }

    public class RentalThresholds
    {
        public double peak { get; set; } = 80;
        public double low { get; set; } = 15;
    }

    public class RentalProduct
    {
        public int productId { get; set; }
        public string? name { get; set; }
        public string? granularity { get; set; }
        public string? unit { get; set; }
        public int? capacity { get; set; }
        public double? hoursPerDay { get; set; }
        public double? occupancy { get; set; }
        public double? bookedTime { get; set; }
        public double? offeredTime { get; set; }
        public long revenue { get; set; }
        public long revenueByRentalDate { get; set; }
        public int rentals { get; set; }
        public double? averageDuration { get; set; }
        public int cancellations { get; set; }
    }

    public class RentalWeekday
    {
        public int weekday { get; set; }
        public string? label { get; set; }
        public double? occupancy { get; set; }
        public int rentals { get; set; }
    }

    public sealed class RentalProductRow
    {
        public RentalProductRow(RentalProduct product, string? currency)
        {
            ProductId = product.productId;
            Name = product.name ?? $"Article {product.productId}";
            Occupancy = Math.Clamp((product.occupancy ?? 0) / 100.0, 0, 1);
            OccupancyText = RentalFormat.Percent(product.occupancy);
            var unit = product.unit == "days" ? "j" : "h";
            var parts = new List<string> { $"{product.rentals} location(s)" };
            if (product.averageDuration is { } duration && product.rentals > 0)
                parts.Add($"durée moyenne {duration.ToString("0.#", CultureInfo.CurrentCulture)} {unit}");
            if (product.cancellations > 0)
                parts.Add($"{product.cancellations} annulée(s)");
            DetailText = string.Join(" · ", parts);
            RevenueText = SiteCurrency.FormatCents(product.revenue, null, currency);
            TimeText = product.bookedTime is { } booked && product.offeredTime is { } offered
                ? $"Loué {booked.ToString("0.#", CultureInfo.CurrentCulture)} {unit} sur {offered.ToString("0.#", CultureInfo.CurrentCulture)} {unit} offerts" + (product.capacity is > 1 ? $" ({product.capacity} en stock)" : string.Empty)
                : string.Empty;
            Tone = (product.occupancy ?? 0) >= 80 ? 1 : (product.occupancy ?? 0) < 15 ? -1 : 0;
        }

        public int ProductId { get; }
        public string Name { get; }
        public double Occupancy { get; }
        public string OccupancyText { get; }
        public string DetailText { get; }
        public string RevenueText { get; }
        public string TimeText { get; }
        public bool HasTime => TimeText.Length > 0;

        /// <summary>1 : souvent plein (vert), −1 : peu loué (orange), 0 : entre les deux.</summary>
        public int Tone { get; }
        public Microsoft.Maui.Graphics.Color ToneColor => Microsoft.Maui.Graphics.Color.FromArgb(Tone switch { 1 => "#047857", -1 => "#B45309", _ => "#512BD4" });
    }

    public static class RentalFormat
    {
        public static string Percent(double? value) => value is { } v ? v.ToString("0.#", CultureInfo.CurrentCulture) + " %" : "—";
    }

    /// <summary>Plage des statistiques de location : rien (période en cours), une période, ou des dates.</summary>
    public sealed record RentalQuery(int? PeriodId = null, DateTime? From = null, DateTime? To = null)
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
            parts.Add($"locale={locale}");
            return "gestion/locations/statistiques?" + string.Join("&", parts);
        }
    }
}
