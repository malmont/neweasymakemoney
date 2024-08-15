namespace Easymakemoney.Models
{
    public class ListProduct
    {
        public int id { get; set; }
        public required string name { get; set; }
        public required string description { get; set; }
        public string? moreinformations { get; set; }
        public double price { get; set; }
        public double purchasePrice { get; set; }
        public int coefficientMultiplier { get; set; }
        public string? barcode { get; set; }
        public bool isbestseller { get; set; }
        public bool isnewarrival { get; set; }
        public bool isfeatured { get; set; }
        public bool isspecialoffer { get; set; }
        public string? image { get; set; }
        public int quantity { get; set; }
        public string? createdAt { get; set; }
        public string? tags { get; set; }
        public string? slug { get; set; }
        public Styles? style { get; set; }
        public List<ProductVariant>? variants { get; set; }

        public required List<Category> category { get; set; }
        
    }
}