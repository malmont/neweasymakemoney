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
    }
}



