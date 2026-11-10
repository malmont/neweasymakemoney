namespace Easymakemoney.Models
{
    public class ListFournisseur
    {
        public int id { get; set; }
        public required string name { get; set; }
        public string? photo { get; set; }
        public string? adresse { get; set; }
        public string? ville { get; set; }
        public string? pays { get; set; }
        public string? tel { get; set; }
        public  int? typeFournisseur { get; set; }

        /// <summary>Devise d'achat du fournisseur ; null : devise du site.</summary>
        public string? purchaseCurrency { get; set; }

        /// <summary>À l'écriture : devise choisie. À la lecture : devise de ses commandes.</summary>
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? currency { get; set; }
    }
}