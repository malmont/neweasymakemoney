using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>Variante proposée pour une ligne reçue sans variante.</summary>
    public sealed class CommandeReceptionVariant
    {
        public int Id { get; init; }
        public string? Size { get; init; }
        public string? Color { get; init; }

        public string Label
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(Size)) parts.Add($"Taille {Size!.Trim()}");
                if (!string.IsNullOrWhiteSpace(Color)) parts.Add($"Couleur {Color!.Trim()}");
                return parts.Count > 0 ? string.Join(" · ", parts) : $"Variante n° {Id}";
            }
        }
    }

    /// <summary>Ligne d'achat dont l'article a plusieurs variantes : le gérant dit laquelle il a reçue.</summary>
    public sealed class CommandeReceptionNeedsVariant
    {
        public int LineId { get; init; }
        public int? ProductId { get; init; }
        public string? ProductName { get; init; }
        public List<CommandeReceptionVariant> Variants { get; init; } = new();

        /// <summary>Libellés des choix, rendus uniques (deux variantes peuvent porter la même taille et la même couleur).</summary>
        public string[] ChoiceLabels() => Variants
            .Select(v => Variants.Count(o => o.Label == v.Label) > 1 ? $"{v.Label} (n° {v.Id})" : v.Label)
            .ToArray();
    }

    /// <summary>
    /// Réponse de « POST commandes/{id}/reception » (lot R1), lue sans MAUI :
    /// { received: [{ lineId, quantity, movementId }], needsVariant: [{ lineId, product, variants }], lines, commande: { id, remaining, receptionStatus } }.
    /// </summary>
    public sealed class CommandeReception
    {
        public int ReceivedLines { get; init; }
        public int ReceivedQuantity { get; init; }
        public List<CommandeReceptionNeedsVariant> NeedsVariant { get; init; } = new();
        public int? CommandeId { get; init; }
        public int? Remaining { get; init; }

        /// <summary>none | partial | complete.</summary>
        public string? ReceptionStatus { get; init; }

        public static CommandeReception Read(JToken? json)
        {
            if (json is not JObject root)
                return new CommandeReception();

            var receivedLines = 0;
            var receivedQuantity = 0;
            switch (root["received"])
            {
                case JArray received:
                    foreach (var item in received.OfType<JObject>())
                    {
                        receivedLines++;
                        receivedQuantity += InvoiceExtraction.Int(item["quantity"]) ?? 0;
                    }
                    break;
                case JValue number when InvoiceExtraction.Int(number) is { } count:
                    receivedQuantity = count;
                    receivedLines = count > 0 ? 1 : 0;
                    break;
            }

            var needs = new List<CommandeReceptionNeedsVariant>();
            if (root["needsVariant"] is JArray needsVariant)
            {
                foreach (var item in needsVariant.OfType<JObject>())
                {
                    var lineId = InvoiceExtraction.Int(item["lineId"]);
                    if (lineId is not > 0)
                        continue;
                    var variants = (item["variants"] as JArray)?.OfType<JObject>()
                        .Select(v => new CommandeReceptionVariant
                        {
                            Id = InvoiceExtraction.Int(v["id"]) ?? 0,
                            Size = InvoiceExtraction.Text(v["size"]),
                            Color = InvoiceExtraction.Text(v["color"]),
                        })
                        .Where(v => v.Id > 0)
                        .ToList() ?? new List<CommandeReceptionVariant>();
                    needs.Add(new CommandeReceptionNeedsVariant
                    {
                        LineId = lineId.Value,
                        ProductId = InvoiceExtraction.Int(item["product"]?["id"]),
                        ProductName = InvoiceExtraction.Text(item["product"]?["name"]),
                        Variants = variants,
                    });
                }
            }

            var commande = root["commande"] as JObject;
            return new CommandeReception
            {
                ReceivedLines = receivedLines,
                ReceivedQuantity = receivedQuantity,
                NeedsVariant = needs,
                CommandeId = InvoiceExtraction.Int(commande?["id"]),
                Remaining = InvoiceExtraction.Int(commande?["remaining"]),
                ReceptionStatus = InvoiceExtraction.Text(commande?["receptionStatus"]),
            };
        }

        /// <summary>Corps du second appel : les variantes choisies par le gérant, { lines: [{ lineId, variantId }] }.</summary>
        public static JObject VariantChoicesBody(IEnumerable<(int LineId, int VariantId)> choices) => new()
        {
            ["lines"] = new JArray(choices.Select(c => new JObject { ["lineId"] = c.LineId, ["variantId"] = c.VariantId })),
        };

        /// <summary>Message au gérant après une ou deux réceptions (la seconde pour les variantes choisies).</summary>
        public static string Summary(int receivedQuantity, CommandeReception last, int postponed)
        {
            string message;
            if (receivedQuantity > 0)
                message = receivedQuantity == 1
                    ? "Réception enregistrée : 1 article ajouté au stock."
                    : $"Réception enregistrée : {receivedQuantity} articles ajoutés au stock.";
            else
                message = "Rien de nouveau à recevoir : le stock est déjà à jour.";

            if (last.ReceptionStatus == "complete" || last.Remaining == 0)
                message += " La commande est entièrement reçue.";
            else if (last.Remaining is > 0)
                message += last.Remaining == 1 ? " Il reste 1 article à recevoir." : $" Il reste {last.Remaining} articles à recevoir.";
            else if (postponed > 0)
                message += " Certains articles restent à recevoir.";

            return message;
        }
    }
}
