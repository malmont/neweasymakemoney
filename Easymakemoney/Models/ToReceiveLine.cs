using System.Globalization;

namespace Easymakemoney.Models
{
    /// <summary>Ligne d'achat dont il reste à recevoir (GET commandes/lignes/a-recevoir, lot backend R1).</summary>
    public class ToReceiveLine
    {
        public int id { get; set; }
        public int commandeId { get; set; }
        public ToReceiveProduct? product { get; set; }
        public ToReceiveVariant? variant { get; set; }
        public int quantityOrdered { get; set; }
        public int quantityReceived { get; set; }
        public int remaining { get; set; }
        public ToReceiveCommande? commande { get; set; }

        public string Title => product?.name ?? "Article";

        public string VariantText
        {
            get
            {
                if (variant == null)
                    return string.Empty;
                var parts = new[] { variant.size, variant.color }.Where(part => !string.IsNullOrWhiteSpace(part));
                return string.Join(" · ", parts);
            }
        }

        public bool HasVariant => VariantText.Length > 0;

        public string QuantityText => quantityReceived > 0
            ? $"{remaining} à recevoir sur {quantityOrdered} ({quantityReceived} déjà reçus)"
            : $"{remaining} à recevoir";
    }

    public class ToReceiveProduct
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? code { get; set; }
    }

    public class ToReceiveVariant
    {
        public int id { get; set; }
        public string? size { get; set; }
        public string? color { get; set; }
    }

    public class ToReceiveCommande
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? date { get; set; }
    }

    /// <summary>Lignes à recevoir d'une même commande.</summary>
    public class ToReceiveGroup : List<ToReceiveLine>
    {
        public ToReceiveGroup(int commandeId, string name, string? date, IEnumerable<ToReceiveLine> lines) : base(lines)
        {
            CommandeId = commandeId;
            Name = name;
            Date = date;
        }

        public int CommandeId { get; }
        public string Name { get; }
        public string? Date { get; }

        public string DateText => DateTime.TryParse(Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? $"Commande du {date:dd/MM/yyyy}"
            : string.Empty;

        public int Remaining => this.Sum(line => line.remaining);
        public string RemainingText => Remaining == 1 ? "1 article à recevoir" : $"{Remaining} articles à recevoir";

        /// <summary>Regroupe les lignes par commande, dans l'ordre de date renvoyé par le backend.</summary>
        public static List<ToReceiveGroup> Build(IEnumerable<ToReceiveLine> lines) => lines
            .GroupBy(line => line.commande?.id ?? line.commandeId)
            .Select(group => new ToReceiveGroup(
                group.Key,
                group.First().commande?.name ?? $"Commande n° {group.Key}",
                group.First().commande?.date,
                group))
            .ToList();
    }
}
