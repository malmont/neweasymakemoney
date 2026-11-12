// Code antérieur aux annotations de nullité : avertissements coupés ici, sans effet sur l'exécution.
#nullable disable warnings
namespace Easymakemoney.Models
{
    public class NoteDeFrais
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string? imageNdf { get; set; }
        public long? montant { get; set; }
        public string date { get; set; }
        public int? typeNoteDeFraisId { get; set; }

        /// <summary>
        /// Voyage d'achat auquel la note est rattachée (lecture seule, lot backend B1a).
        /// Jamais renvoyé à l'enregistrement : le rattachement passe par la route dédiée voyages/rattacher.
        /// </summary>
        public int? tripId { get; set; }

        public bool ShouldSerializetripId() => false;

        public bool HasTrip => tripId.HasValue;

        public bool ShouldSerializeHasTrip() => false;

        /// <summary>
        /// Titre affiché : le serveur ne renvoie pas « name » (seulement « description ») ; sans ce repli, la ligne était vide.
        /// </summary>
        public string DisplayTitle => !string.IsNullOrWhiteSpace(name) ? name : description ?? string.Empty;

        public bool ShouldSerializeDisplayTitle() => false;
    }
}



