using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Étiquettes d'une commande fournisseur reçue (backend CB-2, GET /api/commandes/{id}/etiquettes) : une ligne par variante reçue,
    /// le nombre proposé (modifiable), les codes manquants à générer d'abord. Aucune de ces routes n'écrit de stock.
    /// </summary>
    public class ReceptionLabels
    {
        public ReceptionLabelsCommande? commande { get; set; }
        public string? currency { get; set; }
        public bool pricesIncludeTax { get; set; }
        public int maxLabels { get; set; } = 500;
        public List<ReceptionLabel> labels { get; set; } = new();
        public ReceptionLabelsMissing missing { get; set; } = new();
        public List<ReceptionLabelSkipped> skipped { get; set; } = new();
        public List<ReceptionGeneratedCode>? generated { get; set; }

        public bool HasMissing => missing.count > 0;
        public string MissingButtonText => $"Générer les codes manquants ({missing.count})";
        public bool IsEmpty => labels.Count == 0;

        public string SkippedText => skipped.Count switch
        {
            0 => string.Empty,
            1 => "1 ligne sans variante n'a pas d'étiquette possible.",
            _ => $"{skipped.Count} lignes sans variante n'ont pas d'étiquette possible.",
        };

        public bool HasSkipped => skipped.Count > 0;
    }

    public class ReceptionLabelsCommande
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? receptionStatus { get; set; }
    }

    public class ReceptionLabelsMissing
    {
        public int count { get; set; }
        public List<int> variantIds { get; set; } = new();
    }

    public class ReceptionGeneratedCode
    {
        public int variantId { get; set; }
        public string? barcode { get; set; }
    }

    public class ReceptionLabelSkipped
    {
        public int lineId { get; set; }
        public int? quantityReceived { get; set; }
        public string? reason { get; set; }
    }

    public partial class ReceptionLabel : ObservableObject
    {
        public int variantId { get; set; }
        public string? title { get; set; }
        public string? subtitle { get; set; }
        public string? barcode { get; set; }
        public string? barcodeOf { get; set; }
        public string? symbology { get; set; }
        public long? priceCents { get; set; }
        public bool priceIncludesTax { get; set; }
        public int? quantityReceived { get; set; }
        public bool printable { get; set; }
        public string? reason { get; set; }

        /// <summary>Nombre d'étiquettes voulu ; le serveur propose la quantité reçue, le gérant le change librement.</summary>
        [ObservableProperty]
        private int _copies;

        public string CodeText => string.IsNullOrWhiteSpace(barcode) ? "Pas encore de code-barres" : barcode!;

        public string DetailText
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(subtitle))
                    parts.Add(subtitle!);
                parts.Add(quantityReceived.HasValue ? $"{quantityReceived} reçu(s)" : "quantité reçue inconnue");
                parts.Add(priceCents.HasValue ? SiteCurrency.FormatCents(priceCents.Value) : "sans prix");
                return string.Join(" · ", parts);
            }
        }
    }
}
