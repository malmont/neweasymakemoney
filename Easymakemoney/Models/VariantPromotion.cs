using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Promotion par variante (lot backend VAR, docs/promotions-gestion.md § 8) : une remise datée sur une taille ou une couleur précise,
    /// variantes à prix propre comprises. Pendant ses dates, elle remplace la promotion de l'article, sans cumul. Activée par site
    /// (interrupteur du super admin) : éteinte, l'aperçu le signale et l'écriture est refusée (409 VARIANT_PROMOTIONS_OFF).
    /// </summary>
    public sealed class VariantPromotionRequest
    {
        public List<int>? VariantIds { get; init; }
        public List<int>? ProductIds { get; init; }
        public double Percent { get; init; }
        public string Rounding { get; init; } = "none";
        public DateTime StartDate { get; init; } = DateTime.Today;
        public DateTime EndDate { get; init; } = DateTime.Today.AddDays(14);

        public Dictionary<string, object> ToBody()
        {
            var body = new Dictionary<string, object>
            {
                ["percent"] = Percent,
                ["rounding"] = Rounding,
                ["startDate"] = TreasuryDates.Iso(StartDate),
                ["endDate"] = TreasuryDates.Iso(EndDate),
            };
            if (VariantIds is { Count: > 0 } variants)
                body["variantIds"] = variants;
            else if (ProductIds is { Count: > 0 } products)
                body["productIds"] = products;
            return body;
        }

        public Dictionary<string, object> ToApplyBody(string previewHash, bool allowBelowCost, bool allowBigChange, bool allowReplace, bool allowAboveProductPromotion)
        {
            var body = ToBody();
            body["previewHash"] = previewHash;
            if (allowBelowCost)
                body["allowBelowCost"] = true;
            if (allowBigChange)
                body["allowBigChange"] = true;
            if (allowReplace)
                body["allowReplace"] = true;
            if (allowAboveProductPromotion)
                body["allowAboveProductPromotion"] = true;
            return body;
        }
    }

    public class VariantPromotionPreview
    {
        public List<VariantPromotionRow> rows { get; set; } = new();
        public VariantPromotionSummary? summary { get; set; }
        public VariantPromotionPeriod? period { get; set; }
        public bool enabled { get; set; }
        public List<string> warnings { get; set; } = new();
        public bool pricesIncludeTax { get; set; }
        public string? previewHash { get; set; }
        public List<TreasuryCaveat> caveats { get; set; } = new();

        /// <summary>Interrupteur du site éteint : l'aperçu reste possible, l'application est refusée.</summary>
        public bool IsOff => !enabled || warnings.Contains("VARIANT_PROMOTIONS_OFF");

        public string PeriodText => period == null
            ? string.Empty
            : $"Du {TreasuryDates.Short(period.startDate)} au {TreasuryDates.Short(period.endDate)} ({period.days} jours)";

        public string CaveatsText => string.Join("\n", caveats.Where(c => !string.IsNullOrWhiteSpace(c.text)).Select(c => $"• {c.text}"));
        public bool HasCaveats => CaveatsText.Length > 0;

        public int WritableCount(bool allowBelowCost, bool allowBigChange, bool allowReplace, bool allowAbove) =>
            rows.Count(row => row.IsWritable(allowBelowCost, allowBigChange, allowReplace, allowAbove));
    }

    public class VariantPromotionSummary
    {
        public int variants { get; set; }
        public int changed { get; set; }
        public int skipped { get; set; }
    }

    public class VariantPromotionPeriod
    {
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public int days { get; set; }
    }

    public class VariantPromotionRow
    {
        public int variantId { get; set; }
        public int productId { get; set; }
        public string? name { get; set; }
        public string? variantLabel { get; set; }
        public long? basePrice { get; set; }
        public long? promoPrice { get; set; }
        public long? discount { get; set; }
        public double? marginRateBase { get; set; }
        public double? marginRatePromo { get; set; }
        public int? stock { get; set; }
        public bool changed { get; set; }
        public List<string> alerts { get; set; } = new();

        [JsonIgnore]
        public string? Currency { get; set; }

        public bool Has(string alert) => alerts.Contains(alert);

        /// <summary>Lignes jamais écrites : pas un article de vente, pas de prix, ou prix inchangé après l'arrondi.</summary>
        public bool IsBlocked => Has("NOT_A_SALE_ITEM") || Has("PRICE_MISSING") || Has("NO_CHANGE") || !changed;

        public bool IsWritable(bool allowBelowCost, bool allowBigChange, bool allowReplace, bool allowAbove) =>
            !IsBlocked
            && (!Has("BELOW_COST") || allowBelowCost)
            && (!Has("BIG_CHANGE") || allowBigChange)
            && (!Has("PROMOTION_EXISTS") || allowReplace)
            && (!Has("VARIANT_PRICE_ABOVE_PRODUCT_PROMOTION") || allowAbove);

        public string Label => string.IsNullOrWhiteSpace(variantLabel) ? name ?? $"Variante {variantId}" : variantLabel!;

        public string PriceText => basePrice is { } before && promoPrice is { } after && changed
            ? $"{Money(before)} → {Money(after)}"
            : basePrice is { } price ? Money(price) : "—";

        public string MarginText => marginRateBase is null && marginRatePromo is null
            ? "Coût inconnu"
            : $"Marge {Rate(marginRateBase)} → {Rate(marginRatePromo)}";

        public string AlertsText => string.Join(" · ", alerts.Select(AlertLabel).Where(text => text.Length > 0));
        public bool HasAlerts => AlertsText.Length > 0;

        public static string AlertLabel(string code) => code switch
        {
            "NOT_A_SALE_ITEM" => "Pas un article de vente",
            "PRICE_MISSING" => "Sans prix",
            "BELOW_COST" => "Sous le coût",
            "BIG_CHANGE" => "Remise de plus de 50 %",
            "PROMOTION_EXISTS" => "Déjà une promotion sur ces dates",
            "VARIANT_PRICE_ABOVE_PRODUCT_PROMOTION" => "Plus cher que la promotion de l'article",
            "PRODUCT_PROMOTION_ACTIVE" => "Remplace la promotion de l'article pendant ces dates",
            "COST_UNKNOWN" => "Coût inconnu",
            "NO_CHANGE" => "Prix inchangé après l'arrondi",
            _ => string.Empty,
        };

        private string Money(long cents) => SiteCurrency.FormatCents(cents, null, Currency);
        private static string Rate(double? rate) => rate is { } r ? r.ToString("0.#", CultureInfo.CurrentCulture) + " %" : "—";
    }

    public class VariantPromotionApplyResult
    {
        public VariantPromotionBatch? batch { get; set; }
        public List<VariantPromotionSkipped> skipped { get; set; } = new();
    }

    public class VariantPromotionBatch
    {
        public int id { get; set; }
    }

    public class VariantPromotionSkipped
    {
        public int variantId { get; set; }
        public List<string> alerts { get; set; } = new();
    }

    public class VariantPromotionItem
    {
        public int id { get; set; }
        public int batchId { get; set; }
        public int variantId { get; set; }
        public int productId { get; set; }
        public string? name { get; set; }
        public string? variantLabel { get; set; }
        public double percent { get; set; }
        public long price { get; set; }
        public long basePrice { get; set; }
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public string? status { get; set; }
        public string? endedAt { get; set; }

        [JsonIgnore]
        public string? Currency { get; set; }

        public string Title => string.IsNullOrWhiteSpace(variantLabel) ? $"{name}" : $"{name} · {variantLabel}";
        public string PercentText => percent.ToString("0.##", CultureInfo.CurrentCulture) + " %";
        public string PriceText => $"−{PercentText} : {SiteCurrency.FormatCents(basePrice, null, Currency)} → {SiteCurrency.FormatCents(price, null, Currency)}";
        public string DatesText => $"Du {TreasuryDates.Short(startDate)} au {TreasuryDates.Short(endDate)}";

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
    }
}
