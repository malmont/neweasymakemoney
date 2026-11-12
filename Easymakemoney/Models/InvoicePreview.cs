using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Lot backend APERÇU-FACT : POST /api/gestion/documents/{id}/apercu. Prix de vente prévu, marge et totaux des lignes corrigées,
    /// calculés par le serveur avec la même fonction que la confirmation. Remplace les calculs locaux du mobile.
    /// </summary>
    public class InvoicePreview
    {
        public string? currency { get; set; }
        public string? invoiceCurrency { get; set; }
        public double? exchangeRate { get; set; }
        public bool pricesIncludeTax { get; set; }
        public List<InvoicePreviewLine> lines { get; set; } = new();
        public InvoicePreviewTotals? totals { get; set; }
        public List<InvoicePreviewIssue> issues { get; set; } = new();

        public InvoicePreviewLine? Line(int n) => lines.FirstOrDefault(line => line.n == n);

        /// <summary>Texte du prix de vente d'une ligne, tel que l'appli l'affiche.</summary>
        public string PriceText(InvoicePreviewLine line)
        {
            var culture = CultureInfo.GetCultureInfo("fr-CA");
            string Money(long cents) => SiteCurrency.FormatCents(cents, CultureInfo.CurrentCulture, currency);
            var taxes = pricesIncludeTax ? " taxes comprises" : string.Empty;
            return line.kind switch
            {
                "existing" => line.currentPrice is { } current ? $"Prix de vente : inchangé ({Money(current)}{taxes})" : "Prix de vente : inchangé",
                "create" when line.plannedPrice is { } price =>
                    $"Prix de vente : {Money(price)}{taxes}"
                    + (line.plannedMargin is { } margin && line.plannedMarginRate is { } rate
                        ? $" · marge {Money(margin)} ({rate.ToString("0.#", culture)} %)"
                        : string.Empty),
                "create" => "Prix de vente : à compléter (voir la ligne)",
                _ => "Prix de vente : article à choisir",
            };
        }

        /// <summary>Total des lignes corrigées, dans la devise de la facture.</summary>
        public string? LinesTotalText => totals is null ? null : SiteCurrency.FormatCents(totals.lines, CultureInfo.CurrentCulture, invoiceCurrency);
    }

    public class InvoicePreviewLine
    {
        public int n { get; set; }
        public double quantity { get; set; }
        public long? unitCost { get; set; }
        public long? total { get; set; }
        public long? unitCostConverted { get; set; }
        public string kind { get; set; } = "unresolved";
        public double? coefficient { get; set; }
        public long? plannedPrice { get; set; }
        public long? currentPrice { get; set; }
        public long? plannedMargin { get; set; }
        public double? plannedMarginRate { get; set; }
        public string? groupKey { get; set; }
    }

    public class InvoicePreviewTotals
    {
        public long lines { get; set; }
        public long? shipping { get; set; }
        public double? taxRate { get; set; }
        public long? taxes { get; set; }
        public long? total { get; set; }
        public long? difference { get; set; }
    }

    public class InvoicePreviewIssue
    {
        public string path { get; set; } = string.Empty;
        public string code { get; set; } = string.Empty;
    }
}
