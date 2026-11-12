namespace Easymakemoney.Models
{
    public class FraisDePort
    {
        public int id { get; set; }
        public required string name { get; set; }
        public long? price { get; set; }
        public string? facture { get; set; }
        public string? tracknumber { get; set; }
        public required string image { get; set; }
        public Transporteur? transporteur { get; set; }

        // Lecture seulement (lot B3 bis) : devise de la commande, devise du site et prix converti par le serveur.
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? currency { get; set; }
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? siteCurrency { get; set; }
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public long? priceConverted { get; set; }
    }
}