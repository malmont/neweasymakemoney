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
    }
}
