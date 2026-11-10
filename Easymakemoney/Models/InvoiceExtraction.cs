using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>Article du catalogue proposé par le backend pour une ligne de facture.</summary>
    public sealed class InvoiceProductMatch
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public double? Score { get; set; }
        public int? VariantId { get; set; }
    }

    /// <summary>Fournisseur proposé par le backend quand il n'est pas reconnu avec certitude.</summary>
    public sealed class InvoiceSupplierCandidate
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public double? Score { get; set; }
    }

    /// <summary>Une ligne lue sur la facture. Montants en cents, hors taxes, dans la devise du document.</summary>
    public sealed class InvoiceExtractedLine
    {
        public int N { get; set; }
        public string? Label { get; set; }
        public string? BaseLabel { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string? SupplierCode { get; set; }
        public int? Quantity { get; set; }
        public long? UnitCost { get; set; }
        public long? Total { get; set; }
        public long? UnitCostRead { get; set; }
        public string? GroupKey { get; set; }
        public int GroupSize { get; set; } = 1;
        public InvoiceProductMatch? Product { get; set; }
        public List<InvoiceProductMatch> Candidates { get; set; } = new();

        /// <summary>Confiance propre à la ligne, si le backend la donne un jour.</summary>
        public double? Confidence { get; set; }

        public string DisplayLabel =>
            !string.IsNullOrWhiteSpace(BaseLabel) ? BaseLabel!.Trim()
            : !string.IsNullOrWhiteSpace(Label) ? Label!.Trim()
            : $"Ligne {N}";
    }

    /// <summary>
    /// Extraction d'une facture fournisseur (contrat « lecture-documents-gestion », type facture), lue sans MAUI.
    /// Tout champ absent ou d'un type inattendu est simplement ignoré.
    /// </summary>
    public sealed class InvoiceExtraction
    {
        public string? SupplierName { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierCurrency { get; set; }
        public List<InvoiceSupplierCandidate> SupplierCandidates { get; set; } = new();

        public string? InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? InvoiceCurrency { get; set; }
        public string? AmountsMode { get; set; }

        /// <summary>Devise du site, rendue à la racine de l'extraction.</summary>
        public string? SiteCurrency { get; set; }

        public List<InvoiceExtractedLine> Lines { get; set; } = new();

        public string? ShippingLabel { get; set; }
        public long? ShippingAmount { get; set; }

        public long? TotalRead { get; set; }
        public long? ComputedTotal { get; set; }
        public long? ComputedLinesTotal { get; set; }
        public long? Difference { get; set; }

        public double? ConfidenceOverall { get; set; }
        public double? ConfidenceLines { get; set; }
        public double? ConfidenceTotals { get; set; }

        public List<string> Notes { get; set; } = new();

        public bool IsDuplicate { get; set; }
        public int? DuplicateCommandeId { get; set; }
        public string? DuplicateStatus { get; set; }

        /// <summary>Devise des montants lus : celle de la facture, sinon celle du fournisseur, sinon celle du site.</summary>
        public string? DocumentCurrency => Upper(InvoiceCurrency) ?? Upper(SupplierCurrency) ?? Upper(SiteCurrency);

        public static InvoiceExtraction Read(JObject? json)
        {
            var result = new InvoiceExtraction();
            if (json == null)
                return result;

            var supplier = json["supplier"] as JObject;
            result.SupplierName = Text(supplier?["name"]);
            result.SupplierId = Int(supplier?["id"]);
            result.SupplierCurrency = Upper(Text(supplier?["currency"]));
            if (supplier?["candidates"] is JArray supplierCandidates)
            {
                foreach (var item in supplierCandidates.OfType<JObject>())
                {
                    var id = Int(item["id"]);
                    if (id is > 0)
                        result.SupplierCandidates.Add(new InvoiceSupplierCandidate { Id = id.Value, Name = Text(item["name"]), Score = Double(item["score"]) });
                }
            }

            var invoice = json["invoice"] as JObject;
            result.InvoiceNumber = Text(invoice?["number"]);
            result.InvoiceDate = Date(invoice?["date"]);
            result.InvoiceCurrency = Upper(Text(invoice?["currency"]));
            result.AmountsMode = Text(json["amountsMode"]);
            result.SiteCurrency = Upper(Text(json["currency"]));

            if (json["lines"] is JArray lines)
            {
                var index = 0;
                foreach (var item in lines.OfType<JObject>())
                {
                    index++;
                    result.Lines.Add(ReadLine(item, index));
                }
            }

            if (json["shipping"] is JObject shipping)
            {
                result.ShippingLabel = Text(shipping["label"]);
                result.ShippingAmount = Long(shipping["amount"]);
            }

            result.TotalRead = Long(json["totals"]?["total"]);
            var computed = json["computed"] as JObject;
            result.ComputedTotal = Long(computed?["total"]);
            result.ComputedLinesTotal = Long(computed?["linesTotal"]);
            result.Difference = Long(computed?["difference"]);

            var confidence = json["confidence"] as JObject;
            result.ConfidenceOverall = Double(confidence?["overall"]);
            result.ConfidenceLines = Double(confidence?["lines"]);
            result.ConfidenceTotals = Double(confidence?["totals"]);

            if (json["notes"] is JArray notes)
            {
                foreach (var note in notes)
                {
                    var text = Text(note);
                    if (!string.IsNullOrWhiteSpace(text))
                        result.Notes.Add(text!);
                }
            }

            if (json["duplicate"] is JObject duplicate)
            {
                result.IsDuplicate = true;
                result.DuplicateCommandeId = Int(duplicate["commandeId"]);
                result.DuplicateStatus = Text(duplicate["status"]);
            }

            return result;
        }

        private static InvoiceExtractedLine ReadLine(JObject item, int index)
        {
            var line = new InvoiceExtractedLine
            {
                N = Int(item["n"]) ?? index,
                Label = Text(item["label"]),
                BaseLabel = Text(item["baseLabel"]),
                Size = Text(item["size"]),
                Color = Text(item["color"]),
                SupplierCode = Text(item["supplierCode"]),
                Quantity = Int(item["quantity"]),
                UnitCost = Long(item["unitCost"]),
                Total = Long(item["total"]),
                UnitCostRead = Long(item["unitCostRead"]),
                GroupKey = Text(item["groupKey"]),
                GroupSize = Int(item["groupSize"]) ?? 1,
                Confidence = Double(item["confidence"]),
            };

            if (item["product"] is JObject product && Int(product["id"]) is > 0)
                line.Product = ReadProduct(product);

            if (item["candidates"] is JArray candidates)
            {
                foreach (var candidate in candidates.OfType<JObject>())
                {
                    if (Int(candidate["id"]) is > 0)
                        line.Candidates.Add(ReadProduct(candidate));
                }
            }

            return line;
        }

        private static InvoiceProductMatch ReadProduct(JObject product) => new()
        {
            Id = Int(product["id"]) ?? 0,
            Name = Text(product["name"]),
            Code = Text(product["code"]),
            Score = Double(product["score"]),
            VariantId = Int(product["variantId"]),
        };

        private static string? Upper(string? code) =>
            string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();

        internal static string? Text(JToken? token)
        {
            if (token == null || token.Type is JTokenType.Null or JTokenType.Undefined or JTokenType.Object or JTokenType.Array)
                return null;
            // Newtonsoft transforme les chaînes « 2026-11-01 » en dates : on les remet au format ISO.
            if (token is JValue { Value: DateTime dateTime })
                return dateTime.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            if (token is JValue { Value: DateTimeOffset dateTimeOffset })
                return dateTimeOffset.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            var text = token.Type == JTokenType.String ? token.Value<string>() : Convert.ToString(((JValue)token).Value, CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        internal static double? Double(JToken? token)
        {
            if (token is not JValue value || value.Value == null)
                return null;
            return value.Type switch
            {
                JTokenType.Integer or JTokenType.Float => Convert.ToDouble(value.Value, CultureInfo.InvariantCulture),
                JTokenType.String when double.TryParse(value.Value<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => null,
            };
        }

        internal static long? Long(JToken? token)
        {
            var number = Double(token);
            return number.HasValue ? (long)Math.Round(number.Value, MidpointRounding.AwayFromZero) : null;
        }

        internal static int? Int(JToken? token)
        {
            var number = Double(token);
            return number.HasValue ? (int)Math.Round(number.Value, MidpointRounding.AwayFromZero) : null;
        }

        private static DateTime? Date(JToken? token)
        {
            var text = Text(token);
            if (text == null)
                return null;
            if (text.Length >= 10 && DateTime.TryParseExact(text[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
            return null;
        }
    }
}
