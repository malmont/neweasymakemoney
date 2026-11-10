using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>Document lu par l'IA du backend : facture, reçu ou facture de transport.</summary>
    public sealed class GestionDocument
    {
        public int id { get; set; }
        public string? type { get; set; }
        public string? status { get; set; }
        public string? error { get; set; }
        public JObject? extraction { get; set; }

        /// <summary>Brouillon de vérification (JSON libre), null s'il n'y en a pas.</summary>
        public JToken? draft { get; set; }

        /// <summary>Photos des lignes : objet dont les clés sont les numéros de ligne.</summary>
        public JToken? linePhotos { get; set; }

        public int? commandeId { get; set; }

        public string? createdAt { get; set; }
    }
}
