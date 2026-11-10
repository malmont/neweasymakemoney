namespace Easymakemoney.Models
{
    public class ListCommand : IListItem
    {
        public int id { get; set; }
        public required string name { get; set; }
        public required string photo { get; set; }
        public long? budget { get; set; }
        public required string date { get; set; }
        public int collectionId { get; set; }
        public ListFournisseur? fournisseur { get; set; }

        /// <summary>Devise de la commande ; son budget et ses frais de port sont dans cette devise.</summary>
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? currency { get; set; }

        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double? exchangeRate { get; set; }

        /// <summary>
        /// Voyage d'achat de la commande (lecture seule, lot backend B1a). Jamais renvoyé à l'enregistrement :
        /// le rattachement passe par la route dédiée voyages/rattacher.
        /// </summary>
        public int? tripId { get; set; }

        public bool ShouldSerializetripId() => false;

        public bool HasTrip => tripId.HasValue;

        public bool ShouldSerializeHasTrip() => false;

        public string BudgetText => budget.HasValue
            ? Utilities.SiteCurrency.FormatCents(budget.Value, System.Globalization.CultureInfo.CurrentCulture, currency)
            : string.Empty;

        // Implémentation de l'interface IListItem
        public int Id => id;
        public string Name => name;
        public string Photo => photo;
    }
}
