using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Codes-barres du catalogue dans l'appli de gestion (backend CB-3, docs/codes-barres.md § 8) : un code désigne une seule chose
    /// sur un site (article ou variante), garanti par le serveur ; la génération ne remplace jamais un code.
    /// </summary>
    public class BarcodeSheet
    {
        public string type { get; set; } = "variant";
        public int productId { get; set; }
        public int? variantId { get; set; }
        public string? name { get; set; }
        public string? variantLabel { get; set; }
        public string? barcode { get; set; }
        public string? symbology { get; set; }
        public bool isPos { get; set; }
        public bool isWeb { get; set; }
        public int? stock { get; set; }
        public long? priceCents { get; set; }
        public bool priceIncludesTax { get; set; }

        public bool IsVariant => type == "variant" && variantId.HasValue;

        /// <summary>Identifiant de la fiche qui porte (ou portera) le code : la variante, sinon l'article.</summary>
        public int SheetId => IsVariant ? variantId!.Value : productId;

        public string Title => string.IsNullOrWhiteSpace(variantLabel) ? name ?? "Article" : $"{name} · {variantLabel}";
        public string KindText => IsVariant ? "Variante" : "Article";
        public bool HasBarcode => !string.IsNullOrWhiteSpace(barcode);
        public string CodeText => HasBarcode ? barcode! : "Pas de code-barres";
        public string StockText => stock.HasValue ? $"Stock : {stock.Value}" : "Stock inconnu";

        public string PriceText => priceCents.HasValue
            ? $"{SiteCurrency.FormatCents(priceCents.Value)} {(priceIncludesTax ? "taxes comprises" : "hors taxes")}"
            : "Pas de prix";

        public string ChannelsText => (isPos, isWeb) switch
        {
            (true, true) => "Vendu en caisse et sur le web",
            (true, false) => "Vendu en caisse seulement",
            (false, true) => "Vendu sur le web seulement",
            _ => "Ni en caisse ni sur le web",
        };
    }

    public class BarcodeSearch
    {
        public string? code { get; set; }
        public bool found { get; set; }
        public string? refusal { get; set; }
        public string? currency { get; set; }
        public BarcodeSheet? sheet { get; set; }

        public bool IsFound => found && sheet != null;

        /// <summary>Le code n'est porté par rien et peut être posé sur une fiche.</summary>
        public bool CanAssign => !IsFound && string.IsNullOrEmpty(refusal);

        public string StatusText => IsFound
            ? string.Empty
            : refusal switch
            {
                null or "" => $"Le code {code} n'est porté par aucun article. Vous pouvez le poser sur une fiche.",
                "BARCODE_RESERVED" => $"Le code {code} est réservé aux tickets, avoirs, cartes-cadeaux ou coupons : il ne peut pas désigner un article.",
                "BARCODE_INVALID" => $"Le code {code} n'a pas une forme acceptée : lettres sans accent, chiffres et - . _ /, 64 caractères au plus.",
                _ => $"Le code {code} ne peut pas être posé sur un article.",
            };

        public bool HasStatus => StatusText.Length > 0;
    }

    public class BarcodeMissing
    {
        public int variants { get; set; }
        public int products { get; set; }
        public int total { get; set; }
        public int limit { get; set; } = 500;

        public bool HasMissing => total > 0;

        public string Text => total switch
        {
            0 => "Toutes les fiches ont un code-barres.",
            1 => "1 fiche sans code-barres.",
            _ => $"{total} fiches sans code-barres ({variants} variante(s), {products} article(s) sans variante).",
        };
    }

    public class BarcodeGenerateMissing
    {
        public int written { get; set; }
        public int variants { get; set; }
        public int products { get; set; }
        public int remaining { get; set; }
        public int limit { get; set; }
    }

    public class BarcodeGenerated
    {
        public string? barcode { get; set; }
        public BarcodeSheet? sheet { get; set; }
    }

    public class BarcodeWarning
    {
        public string? code { get; set; }
        public string? message { get; set; }
    }

    public class BarcodeWrite
    {
        public string? barcode { get; set; }
        public bool changed { get; set; }
        public BarcodeWarning? warning { get; set; }
        public BarcodeSheet? sheet { get; set; }
    }

    /// <summary>Une ligne de la planche d'étiquettes : une variante, ou un article sans variante.</summary>
    public sealed record BarcodeLabelRequest(BarcodeSheet Sheet, int Copies);
}
