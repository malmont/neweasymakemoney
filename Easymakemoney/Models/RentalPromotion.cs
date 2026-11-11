using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Promotion de location par article (lot backend LOC3, docs/locations-gestion.md § 3) : un pourcentage sur le tarif, pendant des dates
    /// et des jours de la semaine choisis. La remise dépend du jour de DÉBUT de la location ; elle s'applique en boutique et en caisse.
    /// </summary>
    public sealed class RentalPromotionRequest
    {
        public List<int>? ProductIds { get; init; }
        public int? AdviceId { get; init; }
        public int Percent { get; init; }
        public DateTime StartDate { get; init; } = DateTime.Today;
        public DateTime EndDate { get; init; } = DateTime.Today.AddDays(29);

        /// <summary>1 lundi … 7 dimanche ; vide ou null = tous les jours.</summary>
        public List<int>? Weekdays { get; init; }

        public Dictionary<string, object> ToBody(string? previewHash = null)
        {
            var body = new Dictionary<string, object>
            {
                ["percent"] = Percent,
                ["startDate"] = TreasuryDates.Iso(StartDate),
                ["endDate"] = TreasuryDates.Iso(EndDate),
            };
            if (AdviceId is { } adviceId)
                body["adviceId"] = adviceId;
            else if (ProductIds is { Count: > 0 } ids)
                body["productIds"] = ids;
            if (Weekdays is { Count: > 0 and < 7 } days)
                body["weekdays"] = days.Distinct().OrderBy(d => d).ToList();
            if (previewHash != null)
                body["previewHash"] = previewHash;
            return body;
        }
    }

    public class RentalPromotionPreview
    {
        public int percent { get; set; }
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public int days { get; set; }
        public List<int> weekdays { get; set; } = new();
        public int? adviceId { get; set; }
        public List<RentalPromotionPreviewItem> items { get; set; } = new();
        public RentalPromotionCounts counts { get; set; } = new();
        public List<string> warnings { get; set; } = new();
        public string? previewHash { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        /// <summary>Tout ou rien : on n'applique que si aucun article n'est en conflit ou non louable.</summary>
        public bool CanApply => counts.ok > 0 && counts.overlap == 0 && counts.notRentable == 0;

        public string PeriodText =>
            $"−{percent} % du {TreasuryDates.Short(startDate)} au {TreasuryDates.Short(endDate)} ({days} jours), {RentalWeekdays.Text(weekdays)}.";

        public bool HasBigDiscount => warnings.Contains("BIG_DISCOUNT");

        public string BlockingText => counts switch
        {
            { overlap: > 0 } => "Un article a déjà une promotion sur ces jours : retirez-le ou changez les dates, puis refaites l'aperçu.",
            { notRentable: > 0 } => "Un article ne se loue pas : retirez-le, puis refaites l'aperçu.",
            { ok: 0 } => "Aucun article à mettre en promotion.",
            _ => string.Empty,
        };

        public bool HasBlocking => BlockingText.Length > 0;

        public List<RentalPromotionPreviewRow> Rows() => items.Select(item => new RentalPromotionPreviewRow(item, Currency)).ToList();
    }

    public class RentalPromotionPreviewItem
    {
        public int productId { get; set; }
        public string? name { get; set; }
        public string? status { get; set; }
        public List<int> conflicts { get; set; } = new();
        public RentalPromotionExample? example { get; set; }
    }

    public class RentalPromotionExample
    {
        public string? durationType { get; set; }
        public long rate { get; set; }
        public long discounted { get; set; }
    }

    public class RentalPromotionCounts
    {
        public int ok { get; set; }
        public int notRentable { get; set; }
        public int overlap { get; set; }
    }

    public sealed class RentalPromotionPreviewRow
    {
        public RentalPromotionPreviewRow(RentalPromotionPreviewItem item, string? currency)
        {
            Name = item.name ?? $"Article {item.productId}";
            IsOk = item.status == "ok";
            StatusText = item.status switch
            {
                "ok" => string.Empty,
                "PROMOTION_OVERLAP" => "Déjà en promotion sur ces jours",
                "NOT_RENTABLE" => "Ne se loue pas",
                _ => item.status ?? string.Empty,
            };
            var unit = item.example?.durationType switch { "day" => "par jour", "hour" => "de l'heure", "week" => "par semaine", _ => string.Empty };
            ExampleText = item.example is { } example
                ? $"{SiteCurrency.FormatCents(example.rate, null, currency)} → {SiteCurrency.FormatCents(example.discounted, null, currency)} {unit}".TrimEnd()
                : "Pas de grille de tarifs";
        }

        public string Name { get; }
        public bool IsOk { get; }
        public string StatusText { get; }
        public bool HasStatus => StatusText.Length > 0;
        public string ExampleText { get; }
    }

    public class RentalPromotionItem
    {
        public int id { get; set; }
        public int batchId { get; set; }
        public int productId { get; set; }
        public string? name { get; set; }
        public int percent { get; set; }
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public List<int> weekdays { get; set; } = new();
        public string? status { get; set; }
        public string? endedAt { get; set; }

        public string Title => $"{name} · −{percent} %";
        public string DatesText => $"Du {TreasuryDates.Short(startDate)} au {TreasuryDates.Short(endDate)}, {RentalWeekdays.Text(weekdays)}";
        /// <summary>Terminée par le gérant : une promotion en cours reste valable jusqu'à la fin de la journée du magasin.</summary>
        public bool IsEnding => endedAt != null && status == "active";

        public string StatusText => status switch
        {
            "active" when IsEnding => "Se termine ce soir",
            "active" => "En cours",
            "upcoming" => "À venir",
            "ended" => "Terminée",
            "cancelled" => "Annulée",
            _ => status ?? string.Empty,
        };
        public bool CanEnd => status is "active" or "upcoming" && endedAt == null;

        /// <summary>Affichée dans la liste : en cours, à venir, ou terminée aujourd'hui (jusqu'à ce soir).</summary>
        public bool IsVisible => status is "active" or "upcoming";
    }

    public class RentalPromotionApplyResult
    {
        public RentalPromotionBatch? batch { get; set; }
    }

    public class RentalPromotionBatch
    {
        public int id { get; set; }
        public int percent { get; set; }
        public List<RentalPromotionItem> promotions { get; set; } = new();
    }

    public static class RentalWeekdays
    {
        private static readonly string[] Names = { "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi", "dimanche" };

        /// <summary>« tous les jours », « le samedi et le dimanche », « du lundi au jeudi », « le lundi, le mercredi et le vendredi ».</summary>
        public static string Text(IReadOnlyCollection<int>? weekdays)
        {
            var days = (weekdays ?? Array.Empty<int>()).Where(d => d is >= 1 and <= 7).Distinct().OrderBy(d => d).ToList();
            if (days.Count is 0 or 7)
                return "tous les jours";
            if (days.Count >= 3 && days[^1] - days[0] == days.Count - 1)
                return $"du {Names[days[0] - 1]} au {Names[days[^1] - 1]}";
            var parts = days.Select(d => $"le {Names[d - 1]}").ToList();
            return parts.Count == 1 ? parts[0] : $"{string.Join(", ", parts.Take(parts.Count - 1))} et {parts[^1]}";
        }

        public static string Name(int weekday) => weekday is >= 1 and <= 7 ? Names[weekday - 1] : weekday.ToString(CultureInfo.InvariantCulture);
    }
}
