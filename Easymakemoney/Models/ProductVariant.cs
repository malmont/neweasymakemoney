namespace Easymakemoney.Models
{
    public class ProductVariant
    {
        public int id { get; set; }
        public required ColorsProduct color { get; set; }
        public required SizeProduct size { get; set; }
        public int stockQuantity { get; set; }
       
    }
}