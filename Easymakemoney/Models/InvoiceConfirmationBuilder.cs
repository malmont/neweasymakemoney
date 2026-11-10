using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>Ce que la vérification sait des devises, et les options de la confirmation.</summary>
    public sealed class InvoiceConfirmationContext
    {
        /// <summary>Devise du site (SiteCurrency.CurrentCurrency).</summary>
        public string? SiteCurrency { get; set; }

        /// <summary>Devise de la facture (montants lus).</summary>
        public string? DocumentCurrency { get; set; }

        /// <summary>Devise d'achat du fournisseur existant choisi ; null : devise du site.</summary>
        public string? SupplierCurrency { get; set; }

        /// <summary>« yyyy-MM-dd » : classe la commande dans la période de cette date (période de la facture clôturée).</summary>
        public string? ClassificationDate { get; set; }

        /// <summary>Confirmer malgré un doublon (409 INVOICE_ALREADY_CONFIRMED).</summary>
        public bool Force { get; set; }
    }

    public sealed class InvoiceConfirmationResult
    {
        public JObject? Body { get; init; }
        public List<string> Errors { get; init; } = new();
        public bool IsValid => Body != null && Errors.Count == 0;
    }

    /// <summary>
    /// Corps de « POST gestion/documents/{id}/confirmer » pour une facture fournisseur, construit depuis le brouillon vérifié.
    /// Sans MAUI : testé dans Easymakemoney.Tests.
    /// </summary>
    public static class InvoiceConfirmationBuilder
    {
        /// <summary>Devise étrangère à ramener dans la devise du site, ou null si aucun taux n'est demandé.</summary>
        public static string? ForeignCurrency(InvoiceReviewDraft draft, InvoiceConfirmationContext context)
        {
            if (!InvoiceReview.SameCurrency(context.DocumentCurrency, context.SiteCurrency))
                return context.DocumentCurrency!.Trim().ToUpperInvariant();
            if (draft.Supplier.Mode == InvoiceReviewDraft.ModeExisting
                && !InvoiceReview.SameCurrency(context.SupplierCurrency, context.SiteCurrency))
                return context.SupplierCurrency!.Trim().ToUpperInvariant();
            return null;
        }

        public static InvoiceConfirmationResult Build(InvoiceReviewDraft draft, InvoiceConfirmationContext context)
        {
            var errors = new List<string>();
            var body = new JObject();

            // Fournisseur
            var supplier = draft.Supplier;
            if (supplier.Mode == InvoiceReviewDraft.ModeExisting && supplier.Id is > 0)
            {
                body["supplier"] = new JObject { ["id"] = supplier.Id.Value };
            }
            else if (supplier.Mode == InvoiceReviewDraft.ModeNew)
            {
                if (string.IsNullOrWhiteSpace(supplier.Name))
                    errors.Add("Donnez le nom du nouveau fournisseur.");
                if (supplier.TypeFournisseurId is not > 0)
                    errors.Add("Choisissez le type du nouveau fournisseur.");

                var create = new JObject
                {
                    ["name"] = supplier.Name?.Trim(),
                    ["typeFournisseurId"] = supplier.TypeFournisseurId,
                };
                // Un fournisseur créé achète dans la devise de sa facture.
                if (!InvoiceReview.SameCurrency(context.DocumentCurrency, context.SiteCurrency))
                    create["currency"] = context.DocumentCurrency!.Trim().ToUpperInvariant();
                body["supplier"] = new JObject { ["create"] = create };
            }
            else
            {
                errors.Add("Choisissez le fournisseur.");
            }

            // Facture
            var invoice = new JObject();
            if (!string.IsNullOrWhiteSpace(draft.Invoice.Number))
                invoice["number"] = draft.Invoice.Number.Trim();
            if (TryDate(draft.Invoice.Date, out var date))
                invoice["date"] = date;
            else
                errors.Add("Indiquez la date de la facture.");

            var foreign = ForeignCurrency(draft, context);
            if (foreign != null)
            {
                var rate = InvoiceReview.ParsePositive(draft.Invoice.ExchangeRate);
                if (rate == null)
                    errors.Add($"Indiquez le taux de change : 1 {foreign} en {context.SiteCurrency ?? "devise du site"}.");
                else
                    invoice["exchangeRate"] = rate.Value;
            }
            body["invoice"] = invoice;

            if (TryDate(context.ClassificationDate, out var classification))
                body["classificationDate"] = classification;

            // Lignes, dans l'ordre de la facture
            var lines = new JArray();
            var groups = new Dictionary<string, InvoiceReviewLine>(StringComparer.Ordinal);
            var ordered = draft.Lines.OrderBy(l => l.N).ToList();
            if (ordered.Count == 0)
                errors.Add("La facture n'a aucune ligne.");

            foreach (var line in ordered)
            {
                var title = string.IsNullOrWhiteSpace(line.Label) ? $"Ligne {line.N}" : line.Label!.Trim();
                if (!line.Validated)
                {
                    errors.Add($"{title} : ligne à valider.");
                    continue;
                }

                var problem = InvoiceReview.LineProblem(line);
                if (problem != null)
                {
                    errors.Add($"{title} : {problem}");
                    continue;
                }

                var item = new JObject
                {
                    ["n"] = line.N,
                    ["label"] = string.IsNullOrWhiteSpace(line.Label) ? null : line.Label.Trim(),
                    ["quantity"] = InvoiceReview.ParseQuantity(line.Quantity)!.Value,
                    ["unitCost"] = InvoiceReview.ParseCents(line.UnitCost)!.Value,
                };

                if (line.IsExisting)
                {
                    item["productId"] = line.ProductId!.Value;
                    if (line.VariantId is > 0)
                        item["variantId"] = line.VariantId.Value;
                }
                else
                {
                    // Un article par groupKey : nom et coefficient de la première ligne du groupe pour toutes ses lignes.
                    var groupKey = string.IsNullOrWhiteSpace(line.GroupKey) ? null : line.GroupKey.Trim();
                    var leader = line;
                    if (groupKey != null && !groups.TryGetValue(groupKey, out leader!))
                    {
                        groups[groupKey] = line;
                        leader = line;
                    }

                    var create = new JObject { ["name"] = leader.Name!.Trim() };
                    var coefficient = InvoiceReview.ParsePositive(leader.Coefficient);
                    if (coefficient != null)
                        create["coefficient"] = coefficient.Value;
                    if (groupKey != null)
                        create["groupKey"] = groupKey;
                    item["create"] = create;
                }

                if (!string.IsNullOrWhiteSpace(line.Size))
                    item["size"] = line.Size.Trim();
                if (!string.IsNullOrWhiteSpace(line.Color))
                    item["color"] = line.Color.Trim();

                if (item["label"]!.Type == JTokenType.Null)
                    item.Remove("label");
                lines.Add(item);
            }
            body["lines"] = lines;

            // Frais de port de la facture
            if (draft.Shipping.Include)
            {
                var amount = InvoiceReview.ParseCents(draft.Shipping.Amount);
                if (amount == null)
                    errors.Add("Montant des frais de port invalide.");
                else if (amount > 0)
                    body["shipping"] = new JObject { ["amount"] = amount.Value };
            }

            if (context.Force)
                body["force"] = true;

            return errors.Count == 0
                ? new InvoiceConfirmationResult { Body = body }
                : new InvoiceConfirmationResult { Errors = errors };
        }

        private static bool TryDate(string? text, out string date)
        {
            date = string.Empty;
            if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 10)
                return false;
            if (!DateTime.TryParseExact(text.Trim()[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return false;
            date = parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return true;
        }
    }
}
