namespace Easymakemoney.Models

{
    public class ListCommand
    {
        public int id { get; set; }
        public required string name { get; set; }
        public required string photo { get; set; }
        public double budget { get; set; }
        public required string date { get; set; }
        public int collectionId { get; set; }
    }
}