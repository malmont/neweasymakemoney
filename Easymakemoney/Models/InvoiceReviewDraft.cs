using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Brouillon de vérification d'une facture fournisseur, envoyé tel quel par « PATCH …/brouillon » et repris à l'ouverture.
    /// Les montants restent le texte saisi par le gérant (« 12,50 ») : ils ne sont lus qu'à la validation.
    /// </summary>
    public sealed class InvoiceReviewDraft
    {
        public const string ModeExisting = "existing";
        public const string ModeNew = "new";

        [JsonProperty("version")] public int Version { get; set; } = 1;
        [JsonProperty("supplier")] public InvoiceReviewSupplier Supplier { get; set; } = new();
        [JsonProperty("invoice")] public InvoiceReviewInvoice Invoice { get; set; } = new();
        [JsonProperty("shipping")] public InvoiceReviewShipping Shipping { get; set; } = new();
        [JsonProperty("lines")] public List<InvoiceReviewLine> Lines { get; set; } = new();

        public JObject ToJson() => JObject.FromObject(this);

        /// <summary>Brouillon enregistré, ou null s'il est absent ou d'une autre forme (on repart alors de l'extraction).</summary>
        public static InvoiceReviewDraft? FromJson(JToken? token)
        {
            if (token is not JObject json || json["lines"] is not JArray)
                return null;

            try
            {
                // Sans lecture des dates : « 2026-11-01 » doit rester une chaîne.
                var settings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None };
                var draft = JsonConvert.DeserializeObject<InvoiceReviewDraft>(json.ToString(Formatting.None), settings);
                if (draft == null)
                    return null;
                draft.Supplier ??= new InvoiceReviewSupplier();
                draft.Invoice ??= new InvoiceReviewInvoice();
                // « 2026-11-02T00:00:00 » si la date a été relue comme une date en chemin : on garde le jour.
                if (draft.Invoice.Date is { Length: > 10 } longDate)
                    draft.Invoice.Date = longDate[..10];
                draft.Shipping ??= new InvoiceReviewShipping();
                draft.Lines = draft.Lines?.Where(l => l != null).ToList() ?? new List<InvoiceReviewLine>();
                return draft;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }

    public sealed class InvoiceReviewSupplier
    {
        /// <summary>« existing », « new », ou null tant que le gérant n'a pas choisi.</summary>
        [JsonProperty("mode")] public string? Mode { get; set; }
        [JsonProperty("id")] public int? Id { get; set; }
        [JsonProperty("name")] public string? Name { get; set; }
        [JsonProperty("typeFournisseurId")] public int? TypeFournisseurId { get; set; }
    }

    public sealed class InvoiceReviewInvoice
    {
        [JsonProperty("number")] public string? Number { get; set; }

        /// <summary>Date de la facture, « yyyy-MM-dd ».</summary>
        [JsonProperty("date")] public string? Date { get; set; }

        /// <summary>Taux saisi (texte) : 1 unité de la devise de la facture en devise du site.</summary>
        [JsonProperty("exchangeRate")] public string? ExchangeRate { get; set; }
    }

    public sealed class InvoiceReviewShipping
    {
        [JsonProperty("include")] public bool Include { get; set; }
        [JsonProperty("amount")] public string? Amount { get; set; }
    }

    /// <summary>Une ligne telle que le gérant l'a vérifiée.</summary>
    public sealed class InvoiceReviewLine
    {
        /// <summary>Numéro de ligne de l'extraction (sert aussi pour la photo de la ligne).</summary>
        [JsonProperty("n")] public int N { get; set; }
        [JsonProperty("validated")] public bool Validated { get; set; }

        /// <summary>Libellé lu sur la facture.</summary>
        [JsonProperty("label")] public string? Label { get; set; }

        /// <summary>« existing » (article déjà connu) ou « new » (article à créer).</summary>
        [JsonProperty("mode")] public string Mode { get; set; } = InvoiceReviewDraft.ModeNew;
        [JsonProperty("productId")] public int? ProductId { get; set; }
        [JsonProperty("variantId")] public int? VariantId { get; set; }
        [JsonProperty("productName")] public string? ProductName { get; set; }

        /// <summary>Nom de l'article à créer.</summary>
        [JsonProperty("name")] public string? Name { get; set; }
        [JsonProperty("groupKey")] public string? GroupKey { get; set; }

        [JsonProperty("quantity")] public string? Quantity { get; set; }
        [JsonProperty("unitCost")] public string? UnitCost { get; set; }
        [JsonProperty("coefficient")] public string? Coefficient { get; set; }
        [JsonProperty("size")] public string? Size { get; set; }
        [JsonProperty("color")] public string? Color { get; set; }

        [JsonIgnore] public bool IsExisting => Mode == InvoiceReviewDraft.ModeExisting;

        public InvoiceReviewLine Clone() => (InvoiceReviewLine)MemberwiseClone();
    }
}
