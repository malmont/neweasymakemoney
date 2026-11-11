using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Grille de tarifs de location (lot backend LOC4, docs/locations-gestion.md § 4) : rattachée à des catégories, la changer change le prix de
    /// tous les articles louables de ces catégories. Les réservations déjà faites gardent leur prix.
    /// </summary>
    public class RentalGrid
    {
        public int id { get; set; }
        public string? name { get; set; }
        public RentalRates rates { get; set; } = new();
        public List<RentalGridProduct> products { get; set; } = new();

        [JsonIgnore]
        public string? Currency { get; set; }

        public string RatesText => rates.Text(Currency);

        public string ProductsText => products.Count switch
        {
            0 => "Aucun article louable",
            1 => $"1 article : {products[0].name}",
            _ => $"{products.Count} articles : {string.Join(", ", products.Take(3).Select(p => p.name))}{(products.Count > 3 ? "…" : string.Empty)}",
        };

        public bool IsShared => products.Count > 1;
    }

    public class RentalGridProduct
    {
        public int productId { get; set; }
        public string? name { get; set; }
    }

    public class RentalRates
    {
        public long? hour { get; set; }
        public long? halfDay { get; set; }
        public long? day { get; set; }
        public long? week { get; set; }
        public long? month { get; set; }

        public static readonly (string Key, string Label)[] Types =
        {
            ("hour", "l'heure"), ("halfDay", "la demi-journée"), ("day", "la journée"), ("week", "la semaine"), ("month", "le mois"),
        };

        public long? Get(string type) => type switch
        {
            "hour" => hour,
            "halfDay" => halfDay,
            "day" => day,
            "week" => week,
            "month" => month,
            _ => null,
        };

        /// <summary>« 25,00 $ l'heure · 95,00 $ la journée ».</summary>
        public string Text(string? currency) => string.Join(" · ", Types
            .Where(t => Get(t.Key) is not null)
            .Select(t => $"{SiteCurrency.FormatCents(Get(t.Key)!.Value, null, currency)} {t.Label}"));

        public static string Label(string? type) => Types.FirstOrDefault(t => t.Key == type).Label ?? type ?? string.Empty;
    }

    public sealed class RentalGridRequest
    {
        public int RentalPackId { get; init; }
        public int Percent { get; init; }
        public bool RoundToDollar { get; init; }

        public Dictionary<string, object> ToBody(string? previewHash = null, bool allowShared = false)
        {
            var body = new Dictionary<string, object>
            {
                ["rentalPackId"] = RentalPackId,
                ["percent"] = Percent,
                ["rounding"] = RoundToDollar ? "dollar" : "none",
            };
            if (previewHash != null)
                body["previewHash"] = previewHash;
            if (allowShared)
                body["allowShared"] = true;
            return body;
        }
    }

    public class RentalGridPreview
    {
        public RentalGridPack? pack { get; set; }
        public RentalRates before { get; set; } = new();
        public RentalRates after { get; set; } = new();
        public List<RentalGridChange> changes { get; set; } = new();
        public List<RentalGridProduct> products { get; set; } = new();
        public bool shared { get; set; }
        public List<string> warnings { get; set; } = new();
        public string? previewHash { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        public bool HasChanges => changes.Count > 0;
        public bool IsShared => shared || warnings.Contains("SHARED_GRID");
        public bool HasBigChange => warnings.Contains("BIG_CHANGE");

        public string SharedText => IsShared
            ? $"Cette grille sert à {products.Count} articles : tous changeront de prix ({string.Join(", ", products.Select(p => p.name))}). Pour un seul article, préférez une promotion de location."
            : string.Empty;

        public List<string> ChangeLines() => changes.Select(c =>
            $"{char.ToUpperInvariant(RentalRates.Label(c.type)[0])}{RentalRates.Label(c.type)[1..]} : {Money(c.before)} → {Money(c.after)}{(c.percent is { } p ? $" ({(p > 0 ? "+" : "−")}{Math.Abs(p).ToString("0.#", CultureInfo.CurrentCulture)} %)" : string.Empty)}").ToList();

        private string Money(long? cents) => cents is { } v ? SiteCurrency.FormatCents(v, null, Currency) : "aucun tarif";
    }

    public class RentalGridPack
    {
        public int id { get; set; }
        public string? name { get; set; }
    }

    public class RentalGridChange
    {
        public string? type { get; set; }
        public long? before { get; set; }
        public long? after { get; set; }
        public double? percent { get; set; }
    }

    /// <summary>Changement appliqué (journal), annulable 24 h si la grille n'a pas été retouchée.</summary>
    public class RentalGridModification
    {
        public int id { get; set; }
        public RentalGridPack? pack { get; set; }
        public RentalRates before { get; set; } = new();
        public RentalRates after { get; set; } = new();
        public List<int> productIds { get; set; } = new();
        public string? createdAt { get; set; }
        public string? undoUntil { get; set; }
        public string? undoneAt { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        public string Title => pack?.name ?? $"Grille {id}";
        public string SummaryText => $"{before.Text(Currency)}\n→ {after.Text(Currency)}";

        public bool CanUndoAt(DateTimeOffset now) =>
            undoneAt == null && DateTimeOffset.TryParse(undoUntil, CultureInfo.InvariantCulture, DateTimeStyles.None, out var until) && now < until;

        public bool CanUndo => CanUndoAt(DateTimeOffset.UtcNow);

        public string StatusText => undoneAt != null ? "Annulé" : CanUndo ? "Annulable 24 h" : string.Empty;
    }
}
