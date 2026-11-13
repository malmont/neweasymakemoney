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

        /// <summary>IA-FACT-2-A : la commande ouverte à laquelle la facture s'ajoute ; null pour une nouvelle commande.</summary>
        public InvoicePreviewTarget? target { get; set; }

        /// <summary>Ajout à une commande admise (une cible refusée porte refused et un refus dans issues).</summary>
        public bool AddsToOrder => target is { refused: null };

        public InvoicePreviewLine? Line(int n) => lines.FirstOrDefault(line => line.n == n);

        /// <summary>Texte du prix de vente d'une ligne, tel que l'appli l'affiche.</summary>
        public string PriceText(InvoicePreviewLine line)
        {
            var culture = CultureInfo.GetCultureInfo("fr-CA");
            string Money(long cents) => SiteCurrency.FormatCents(cents, CultureInfo.CurrentCulture, currency);
            var taxes = pricesIncludeTax ? " taxes comprises" : string.Empty;
            return line.kind switch
            {
                "existing" when line.price is { changed: true, currentPrice: { } before, newPrice: { } after } =>
                    $"Prix de vente : {Money(before)} → {Money(after)}{taxes}"
                    + (line.plannedMargin is { } newMargin && line.plannedMarginRate is { } newRate
                        ? $" · marge {Money(newMargin)} ({newRate.ToString("0.#", culture)} %)"
                        : string.Empty)
                    + (line.price.belowCost ? " · sous le coût d'achat" : string.Empty),
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

        /// <summary>IA-FACT-2-A : la ligne d'achat de la commande cible avant et après l'ajout (article déjà commandé).</summary>
        public InvoicePreviewOrderLine? lineBefore { get; set; }
        public InvoicePreviewOrderLine? lineAfter { get; set; }

        /// <summary>IA-ACT-5 : mise en ligne d'un article créé (no, now, reception) et ses mentions (STOCK_ZERO, NO_PHOTO).</summary>
        public string? publishPos { get; set; }
        public string? publishWeb { get; set; }
        public List<string> mentions { get; set; } = new();

        /// <summary>IA-ACT-5 : prix d'un article déjà au catalogue, avant et après.</summary>
        public InvoicePreviewLinePrice? price { get; set; }

        /// <summary>La mise en ligne d'un article créé, en clair ; null si le serveur ne la rend pas.</summary>
        public string? PublishText => publishPos == null && publishWeb == null
            ? null
            : $"En caisse : {PublishWord(publishPos)} · Boutique en ligne : {PublishWord(publishWeb)}";

        public string? MentionsText => mentions.Count == 0
            ? null
            : string.Join(" · ", mentions.Select(m => m switch
            {
                "STOCK_ZERO" => "stock 0 : affiché en rupture jusqu'à la réception",
                "NO_PHOTO" => "sans photo sur la boutique",
                _ => m,
            }));

        private static string PublishWord(string? value) => value switch
        {
            "now" => "tout de suite",
            "reception" => "à la réception du stock",
            "no" => "non",
            _ => "—",
        };
    }

    public class InvoicePreviewLinePrice
    {
        public string? mode { get; set; }
        public double? coefficient { get; set; }
        public long? newPrice { get; set; }
        public long? currentPrice { get; set; }
        public bool changed { get; set; }
        public bool belowCost { get; set; }
    }

    public class InvoicePreviewOrderLine
    {
        public double quantity { get; set; }
        public long? unitCost { get; set; }
    }

    public class InvoicePreviewTarget
    {
        public int commandeId { get; set; }
        public string? name { get; set; }
        public string? date { get; set; }
        public string? currency { get; set; }
        public double? exchangeRate { get; set; }
        public long? budgetBefore { get; set; }
        public long? budgetAfter { get; set; }
        public InvoicePreviewShipping? shipping { get; set; }
        public string? refused { get; set; }
    }

    public class InvoicePreviewShipping
    {
        public long? current { get; set; }
        public long? added { get; set; }
        public long? total { get; set; }
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
