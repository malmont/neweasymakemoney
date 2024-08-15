namespace Easymakemoney.Models
{
    public class Category
    {
        public int id { get; set; }
        public required string name { get; set; }

        public required string description { get; set; }

        public required string image { get; set; }
    }
}