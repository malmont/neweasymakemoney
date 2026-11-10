using System.Globalization;
using Easymakemoney.Utilities;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>Aperçu d'un changement de prix (POST gestion/prix/apercu, lot backend P4, docs/prix-gestion.md). Rien n'est écrit.</summary>
    public class PricePreview
    {
        public List<PricePreviewRow> rows { get; set; } = new();
        public PricePreviewSummary summary { get; set; } = new();
        public bool pricesIncludeTax { get; set; }
        public string? previewHash { get; set; }
        public List<TreasuryCaveat> caveats { get; set; } = new();

        public PricePreview WithCurrency(string? currency)
        {
            foreach (var row in rows)
                row.Currency = currency;
            return this;
        }

        public bool HasBelowCost => rows.Any(row => row.Has("BELOW_COST"));
        public bool HasBigChange => rows.Any(row => row.Has("BIG_CHANGE"));

        /// <summary>Lignes qui seront écrites avec les autorisations données (mêmes règles que le backend).</summary>
        public int WritableCount(bool allowBelowCost, bool allowBigChange) => rows.Count(row =>
            row.changed && !row.Has("COST_UNKNOWN") && !row.Has("NO_CHANGE") && !row.Has("NOT_A_SALE_ITEM")
            && (allowBelowCost || !row.Has("BELOW_COST")) && (allowBigChange || !row.Has("BIG_CHANGE")));

        public string SummaryText => $"{summary.products} article(s), {summary.changed} prix à changer, {summary.skipped} laissé(s) tel(s) quel(s)";

        public string CaveatsText => string.Join("\n", caveats.Where(caveat => !string.IsNullOrWhiteSpace(caveat.text)).Select(caveat => $"• {caveat.text}"));
        public bool HasCaveats => CaveatsText.Length > 0;
    }

    public class PricePreviewSummary
    {
        public int products { get; set; }
        public int rows { get; set; }
        public int changed { get; set; }
        public int skipped { get; set; }
    }

    public class PricePreviewRow
    {
        public int productId { get; set; }
        public int? variantId { get; set; }
        public string? level { get; set; }
        public string? name { get; set; }
        public int? inheritingVariants { get; set; }
        public long? oldPrice { get; set; }
        public long? newPrice { get; set; }
        public long? cost { get; set; }
        public double? marginRateBefore { get; set; }
        public double? marginRateAfter { get; set; }
        public int? stock { get; set; }
        public bool changed { get; set; }
        public List<string> alerts { get; set; } = new();

        [JsonIgnore]
        public string? Currency { get; set; }

        public bool Has(string alert) => alerts.Contains(alert);

        public string Title => level == "variant" ? $"{name} (variante)" : name ?? string.Empty;

        public string PriceText => changed
            ? $"{Money(oldPrice)} → {Money(newPrice)}"
            : Money(oldPrice);

        public string MarginText => marginRateBefore is null && marginRateAfter is null
            ? (cost is null ? "Coût inconnu" : string.Empty)
            : $"Marge {Rate(marginRateBefore)} → {Rate(marginRateAfter)}";

        public string AlertsText => string.Join(" · ", alerts.Select(AlertLabel).Where(text => text.Length > 0));
        public bool HasAlerts => AlertsText.Length > 0;
        public bool IsBlocking => Has("BELOW_COST") || Has("BIG_CHANGE");

        public string VariantsText => inheritingVariants is > 0 ? $"S'applique aussi à {inheritingVariants} variante(s) sans prix propre" : string.Empty;
        public bool HasVariantsText => VariantsText.Length > 0;

        public static string AlertLabel(string code) => code switch
        {
            "BELOW_COST" => "Sous le coût",
            "BIG_CHANGE" => "Écart de plus de 50 %",
            "SPECIAL_PRICE_ACTIVE" => "Promotion en cours, son prix reste inchangé",
            "COST_UNKNOWN" => "Coût inconnu : non modifié",
            "NO_CHANGE" => "Prix inchangé",
            "PRICE_MISSING" => "Sans prix : non modifié",
            "NOT_A_SALE_ITEM" => "Location ou abonnement : non modifié",
            _ => string.Empty,
        };

        private string Money(long? cents) => cents is { } value ? SiteCurrency.FormatCents(value, null, Currency) : "—";
        private static string Rate(double? rate) => rate is { } value ? value.ToString("0.#", CultureInfo.CurrentCulture) + " %" : "—";
    }

    public class PriceApplyResult
    {
        public int batchId { get; set; }
        public int changed { get; set; }
        public string? undoUntil { get; set; }
    }

    public class PriceUndoResult
    {
        public int restored { get; set; }
        public JToken? skipped { get; set; }
        public bool adviceRestored { get; set; }
    }

    /// <summary>Lot de changements de prix du journal (GET gestion/prix/lots).</summary>
    public class PriceBatch
    {
        public int id { get; set; }
        public string? mode { get; set; }
        public double? value { get; set; }
        public string? rounding { get; set; }
        public int? adviceId { get; set; }
        public int changed { get; set; }
        public JToken? createdBy { get; set; }
        public string? createdAt { get; set; }
        public string? undoneAt { get; set; }
        public bool undoable { get; set; }
        public string? undoBlockedBy { get; set; }
        public string? undoUntil { get; set; }

        public string Title => mode == "coefficient"
            ? $"Coefficient {value?.ToString("0.##", CultureInfo.CurrentCulture)} sur le coût"
            : value is >= 0 ? $"Hausse de {value?.ToString("0.##", CultureInfo.CurrentCulture)} %" : $"Baisse de {Math.Abs(value ?? 0).ToString("0.##", CultureInfo.CurrentCulture)} %";

        public string DetailText
        {
            get
            {
                var author = createdBy?.Type == JTokenType.Object ? createdBy.Value<string>("name") ?? createdBy.Value<string>("email") : createdBy?.ToString();
                var when = DateTime.TryParse(createdAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
                    ? date.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : createdAt;
                return string.IsNullOrWhiteSpace(author) ? $"{changed} prix · {when}" : $"{changed} prix · {when} · {author}";
            }
        }

        public string StatusText => undoneAt != null
            ? "Annulé"
            : undoable ? "Annulable pendant 24 h" : undoBlockedBy switch
            {
                "UNDO_EXPIRED" => "Plus annulable (24 h passées)",
                "PRICE_EDITED_SINCE" => "Des prix ont été retouchés depuis",
                _ => "Plus annulable",
            };

        public bool CanUndo => undoable && undoneAt == null;
    }
}
