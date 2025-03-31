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
    }
}