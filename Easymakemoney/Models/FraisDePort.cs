namespace Easymakemoney.Models
{
    public class FraisDePort
    {
        public int id { get; set; }
        public required string name { get; set; }
        public double? price { get; set; }
        public string? facture { get; set; }
        public string? tracknumber { get; set; }
        public required string image { get; set; }
        public Transporteur? transporteur { get; set; }
    }
}