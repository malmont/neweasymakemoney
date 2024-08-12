namespace Easymakemoney.Models

{
    public class ListCommand
    {
        public int id { get; set; }
        public string name { get; set; }
        public string photo { get; set; }
        public double budget { get; set; }
        public string date { get; set; }
        public int collectionId { get; set; }
    }
}