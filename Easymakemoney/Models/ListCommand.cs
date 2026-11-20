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

        /// <summary>
        /// Commande d'arrivée d'un transfert de stock entre boutiques du groupe (lot backend G-5, lecture seule) :
        /// elle ne se modifie pas et ne se supprime pas, mais elle se réceptionne.
        /// </summary>
        public bool groupTransfer { get; set; }

        public bool ShouldSerializegroupTransfer() => false;

        /// <summary>Période de la commande clôturée : posé par l'écran qui affiche la liste (jamais lu ni envoyé).</summary>
        [JsonIgnore]
        public bool PeriodClosed { get; set; }

        /// <summary>Supprimer, rattacher à un voyage, toucher aux lignes et aux articles : voir <see cref="TransferOrderRules.CanEdit"/>.</summary>
        [JsonIgnore]
        public bool CanEdit => TransferOrderRules.CanEdit(PeriodClosed, groupTransfer);

        [JsonIgnore]
        public string TransferBadge => TransferOrderRules.Badge;

        public string BudgetText => budget.HasValue
            ? Utilities.SiteCurrency.FormatCents(budget.Value, System.Globalization.CultureInfo.CurrentCulture, currency)
            : string.Empty;

        // Implémentation de l'interface IListItem
        public int Id => id;
        public string Name => name;
        public string Photo => photo;
    }
}
