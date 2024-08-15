namespace Easymakemoney.Models
{
    public class ProductVariant
    {
        public int id { get; set; }
        public required Color color { get; set; }
        public required Size size { get; set; }
        public int stockQuantity { get; set; }
       
    }
}