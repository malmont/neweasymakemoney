using System.Globalization;
using Easymakemoney.Converters;
using Easymakemoney.Utilities;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Règles de la vérification d'une facture fournisseur, sans MAUI : préremplissage depuis l'extraction,
    /// reprise du brouillon, lignes douteuses, lecture des montants saisis.
    /// </summary>
    public static class InvoiceReview
    {
        public const double DefaultCoefficient = 2.0;

        /// <summary>Sous ce score, la correspondance proposée est à vérifier.</summary>
        public const double SureMatchScore = 0.8;

        /// <summary>Sous cette confiance, la lecture est à vérifier.</summary>
        public const double LowConfidence = 0.7;

        // ----- lecture des saisies -----

        /// <summary>« 12,50 » ou « 12.50 » → 1250 ; null si vide, illisible ou négatif.</summary>
        public static long? ParseCents(string? text)
        {
            var value = DecimalInputConverter.Parse(text);
            if (value is null or < 0)
                return null;
            return MoneyHelper.ToCents(value);
        }

        /// <summary>Quantité entière, 1 au moins.</summary>
        public static int? ParseQuantity(string? text)
        {
            var value = DecimalInputConverter.Parse(text);
            if (value is null or < 1 || value.Value != Math.Floor(value.Value) || value.Value > int.MaxValue)
                return null;
            return (int)value.Value;
        }

        /// <summary>Nombre strictement positif (coefficient, taux de change).</summary>
        public static double? ParsePositive(string? text)
        {
            var value = DecimalInputConverter.Parse(text);
            return value is > 0 ? value : null;
        }

        /// <summary>1250 → « 12,50 » (selon la culture), pour un champ de saisie.</summary>
        public static string FormatAmount(long cents, CultureInfo? culture = null) =>
            (cents / 100m).ToString("0.00", culture ?? CultureInfo.CurrentCulture);

        public static string FormatNumber(double value, CultureInfo? culture = null) =>
            value.ToString("0.######", culture ?? CultureInfo.CurrentCulture);

        public static bool SameCurrency(string? a, string? b) =>
            string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)
            || string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

        // ----- lignes douteuses -----

        /// <summary>Ligne à regarder en premier : sans correspondance sûre, montant non lu, ou lecture peu sûre.</summary>
        public static bool IsDoubtful(InvoiceExtractedLine line) => DoubtReason(line) != null;

        public static string? DoubtReason(InvoiceExtractedLine line)
        {
            if (line.UnitCost is null)
                return "Prix non lu : saisissez-le.";
            if (line.Confidence is < LowConfidence)
                return "Lecture incertaine : vérifiez cette ligne.";
            if (line.Product == null)
                return line.Candidates.Count > 0
                    ? "Article peut-être déjà connu : vérifiez."
                    : "Aucun article correspondant : il sera créé.";
            if (line.Product.Score is < SureMatchScore)
                return "Correspondance incertaine : vérifiez l'article.";
            return null;
        }

        /// <summary>Lignes douteuses d'abord, puis l'ordre de la facture.</summary>
        public static List<InvoiceExtractedLine> OrderForReview(IEnumerable<InvoiceExtractedLine> lines) =>
            lines.Select((line, index) => (line, index))
                 .OrderBy(x => IsDoubtful(x.line) ? 0 : 1)
                 .ThenBy(x => x.index)
                 .Select(x => x.line)
                 .ToList();

        // ----- préremplissage et reprise -----

        /// <summary>Ligne préremplie depuis l'extraction : article reconnu → « déjà connu », sinon « nouvel article ».</summary>
        public static InvoiceReviewLine InitialLine(InvoiceExtractedLine line, CultureInfo? culture = null)
        {
            var review = new InvoiceReviewLine
            {
                N = line.N,
                Label = line.Label ?? line.BaseLabel,
                Name = line.DisplayLabel,
                GroupKey = line.GroupKey,
                Quantity = (line.Quantity is > 0 ? line.Quantity.Value : 1).ToString(CultureInfo.InvariantCulture),
                UnitCost = line.UnitCost.HasValue ? FormatAmount(line.UnitCost.Value, culture) : null,
                Coefficient = FormatNumber(DefaultCoefficient, culture),
                Size = line.Size,
                Color = line.Color,
            };

            if (line.Product != null)
            {
                review.Mode = InvoiceReviewDraft.ModeExisting;
                review.ProductId = line.Product.Id;
                review.VariantId = line.Product.VariantId;
                review.ProductName = line.Product.Name;
            }

            return review;
        }

        /// <summary>Brouillon de départ : ce qui a été lu, rien de validé.</summary>
        public static InvoiceReviewDraft InitialDraft(InvoiceExtraction extraction, CultureInfo? culture = null)
        {
            var draft = new InvoiceReviewDraft
            {
                Invoice = new InvoiceReviewInvoice
                {
                    Number = extraction.InvoiceNumber,
                    Date = extraction.InvoiceDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                },
                Shipping = new InvoiceReviewShipping
                {
                    Include = extraction.ShippingAmount is > 0,
                    Amount = extraction.ShippingAmount.HasValue ? FormatAmount(extraction.ShippingAmount.Value, culture) : null,
                },
                Lines = extraction.Lines.Select(l => InitialLine(l, culture)).ToList(),
            };

            if (extraction.SupplierId is > 0)
            {
                draft.Supplier = new InvoiceReviewSupplier { Mode = InvoiceReviewDraft.ModeExisting, Id = extraction.SupplierId, Name = extraction.SupplierName };
            }
            else if (extraction.SupplierCandidates.Count > 0)
            {
                // Des fournisseurs proches existent : le gérant choisit, rien n'est présélectionné.
                draft.Supplier = new InvoiceReviewSupplier { Mode = InvoiceReviewDraft.ModeExisting, Name = extraction.SupplierName };
            }
            else
            {
                draft.Supplier = new InvoiceReviewSupplier { Mode = InvoiceReviewDraft.ModeNew, Name = extraction.SupplierName };
            }

            return draft;
        }

        /// <summary>
        /// Reprend le brouillon enregistré sur le brouillon de départ : les lignes se retrouvent par leur numéro,
        /// une ligne absente du brouillon garde la lecture.
        /// </summary>
        public static InvoiceReviewDraft Merge(InvoiceReviewDraft initial, InvoiceReviewDraft? saved)
        {
            if (saved == null)
                return initial;

            var savedLines = saved.Lines
                .GroupBy(l => l.N)
                .ToDictionary(g => g.Key, g => g.First());

            return new InvoiceReviewDraft
            {
                Supplier = saved.Supplier.Mode != null ? saved.Supplier : initial.Supplier,
                Invoice = new InvoiceReviewInvoice
                {
                    Number = saved.Invoice.Number ?? initial.Invoice.Number,
                    Date = saved.Invoice.Date ?? initial.Invoice.Date,
                    ExchangeRate = saved.Invoice.ExchangeRate ?? initial.Invoice.ExchangeRate,
                },
                Shipping = saved.Shipping.Amount != null || saved.Shipping.Include ? saved.Shipping : initial.Shipping,
                Lines = initial.Lines
                    .Select(line => savedLines.TryGetValue(line.N, out var kept) ? kept : line)
                    .ToList(),
            };
        }

        /// <summary>Ce qui empêche de valider la ligne, en une phrase ; null si elle est valable.</summary>
        public static string? LineProblem(InvoiceReviewLine line)
        {
            if (ParseQuantity(line.Quantity) == null)
                return "Quantité invalide : un nombre entier, 1 au moins.";
            if (ParseCents(line.UnitCost) == null)
                return "Coût hors taxes invalide.";
            if (line.IsExisting)
            {
                if (line.ProductId is not > 0)
                    return "Choisissez l'article déjà connu, ou « Nouvel article ».";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(line.Name))
                    return "Donnez un nom au nouvel article.";
                if (!string.IsNullOrWhiteSpace(line.Coefficient) && ParsePositive(line.Coefficient) == null)
                    return "Coefficient invalide.";
            }
            return null;
        }
    }
}
