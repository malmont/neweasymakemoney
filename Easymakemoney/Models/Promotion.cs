using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>Aperçu d'une promotion datée (POST gestion/promotions/apercu, lot backend P5). Rien n'est écrit.</summary>
    public class PromotionPreview
    {
        public List<PromotionPreviewRow> rows { get; set; } = new();
        public PricePreviewSummary summary { get; set; } = new();
        public PromotionPeriod? period { get; set; }
        public bool pricesIncludeTax { get; set; }
        public string? previewHash { get; set; }
        public List<TreasuryCaveat> caveats { get; set; } = new();

        public bool HasBelowCost => rows.Any(row => row.Has("BELOW_COST"));
        public bool HasBigChange => rows.Any(row => row.Has("BIG_CHANGE"));
        public bool HasExisting => rows.Any(row => row.Has("PROMOTION_EXISTS"));

        /// <summary>Lignes qui seront écrites avec les accords donnés (mêmes règles que le backend).</summary>
        public int WritableCount(bool allowBelowCost, bool allowBigChange, bool allowReplace) => rows.Count(row =>
            row.changed && !row.Has("NOT_A_SALE_ITEM") && !row.Has("PRICE_MISSING") && !row.Has("NO_CHANGE")
            && (allowBelowCost || !row.Has("BELOW_COST"))
            && (allowBigChange || !row.Has("BIG_CHANGE"))
            && (allowReplace || !row.Has("PROMOTION_EXISTS")));

        public string PeriodText => period == null
            ? string.Empty
            : $"Du {TreasuryDates.Short(period.startDate)} au {TreasuryDates.Short(period.endDate)} ({period.days} jours). Le prix normal revient tout seul à la fin.";

        public string CaveatsText => string.Join("\n", caveats.Where(caveat => !string.IsNullOrWhiteSpace(caveat.text)).Select(caveat => $"• {caveat.text}"));
        public bool HasCaveats => CaveatsText.Length > 0;
    }

    public class PromotionPeriod
    {
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public int days { get; set; }
    }

    public class PromotionPreviewRow
    {
        public int productId { get; set; }
        public string? name { get; set; }
        public long? regularPrice { get; set; }
        public long? promoPrice { get; set; }
        public double? marginRateRegular { get; set; }
        public double? marginRatePromo { get; set; }
        public int? stock { get; set; }
        public PromotionVariantsOwnPrice? variantsOwnPrice { get; set; }
        public bool changed { get; set; }
        public List<string> alerts { get; set; } = new();

        public bool Has(string alert) => alerts.Contains(alert);

        public string PriceText => promoPrice is { } promo && changed
            ? $"{Money(regularPrice)} → {Money(promo)}"
            : Money(regularPrice);

        public string MarginText => marginRateRegular is null && marginRatePromo is null
            ? "Coût inconnu"
            : $"Marge {Rate(marginRateRegular)} → {Rate(marginRatePromo)} pendant la promotion";

        public string AlertsText => string.Join(" · ", alerts.Select(AlertLabel).Where(text => text.Length > 0));
        public bool HasAlerts => AlertsText.Length > 0;
        public bool IsBlocking => Has("BELOW_COST") || Has("BIG_CHANGE") || Has("PROMOTION_EXISTS");

        /// <summary>Les variantes à prix propre n'ont pas la promotion (règle actuelle de la boutique et de la caisse).</summary>
        public string VariantsText => variantsOwnPrice is { count: > 0 } excluded
            ? $"Pas de promotion sur {excluded.count} variante(s) à prix propre : {string.Join(", ", excluded.variants.Select(v => v.variantLabel ?? $"n° {v.variantId}"))}"
            : string.Empty;

        public bool HasVariantsText => VariantsText.Length > 0;

        public static string AlertLabel(string code) => code switch
        {
            "BELOW_COST" => "Sous le coût",
            "BIG_CHANGE" => "Remise de plus de 50 %",
            "PROMOTION_EXISTS" => "Une promotion existe déjà : elle sera remplacée",
            "COST_UNKNOWN" => "Coût inconnu",
            "NO_CHANGE" => "Inchangé",
            "PRICE_MISSING" => "Sans prix : non modifié",
            "NOT_A_SALE_ITEM" => "Location, abonnement ou véhicule : non modifié",
            _ => string.Empty,
        };

        private static string Money(long? cents) => cents is { } value ? SiteCurrency.FormatCents(value) : "—";
        private static string Rate(double? rate) => rate is { } value ? value.ToString("0.#", CultureInfo.CurrentCulture) + " %" : "—";
    }

    public class PromotionVariantsOwnPrice
    {
        public int count { get; set; }
        public List<PromotionVariantLabel> variants { get; set; } = new();
    }

    public class PromotionVariantLabel
    {
        public int variantId { get; set; }
        public string? variantLabel { get; set; }
    }

    /// <summary>Article en promotion en cours ou à venir (GET gestion/promotions), quelle qu'en soit l'origine.</summary>
    public class PromotionItem
    {
        public int productId { get; set; }
        public string? name { get; set; }
        public long? regularPrice { get; set; }
        public long? specialPrice { get; set; }
        public string? startsAt { get; set; }
        public string? endsAt { get; set; }
        public string? status { get; set; }
        public int? batchId { get; set; }

        public string PriceText => $"{SiteCurrency.FormatCents(regularPrice ?? 0)} → {SiteCurrency.FormatCents(specialPrice ?? 0)}";

        public string DatesText => DatesTextIn(TimeZoneInfo.Local);

        /// <summary>Dates dans un fuseau donné (testable sans dépendre du fuseau de la machine).</summary>
        public string DatesTextIn(TimeZoneInfo zone) => $"Du {Local(startsAt, zone)} au {Local(endsAt, zone)}";

        public string StatusText => status switch
        {
            "active" => "En cours",
            "planned" => "À venir",
            _ => "Terminée",
        };

        public bool CanEnd => batchId.HasValue && status is "active" or "planned";
        public bool FromAdmin => !batchId.HasValue;

        /// <summary>Les bornes sont des instants UTC : affichées au jour local du téléphone.</summary>
        private static string Local(string? iso, TimeZoneInfo zone) =>
            DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? TimeZoneInfo.ConvertTime(date, zone).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                : iso ?? "—";
    }

    public class PromotionEndResult
    {
        public int restored { get; set; }
        public int stopped { get; set; }
        public int over { get; set; }
        public bool adviceRestored { get; set; }
    }
}
