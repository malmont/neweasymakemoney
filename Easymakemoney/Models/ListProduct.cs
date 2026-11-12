namespace Easymakemoney.Models
{
    public class ListProduct : IListItem
    {
        public int id { get; set; }
        public required string name { get; set; }
        public string? description { get; set; }
        public string? moreinformations { get; set; }
        public double? price { get; set; }
        public double? purchasePrice { get; set; }
        public double? coefficientMultiplier { get; set; }
        public string? barcode { get; set; }
        public bool? isbestseller { get; set; }
        public bool? isnewarrival { get; set; }
        public bool? isfeatured { get; set; }
        public bool? isspecialoffer { get; set; }
        public string? image { get; set; }
        public int? quantity { get; set; }
        public int? freezeQuantity { get; set; }
        public string? createdAt { get; set; }
        public string? tags { get; set; }
        public string? slug { get; set; }
        public Styles? style { get; set; }
        public ObservableCollection<ProductVariant>? variants { get; set; }

        public List<Category>? category { get; set; }
         // Implémentation de l'interface IListItem
        public int Id => id;
        public string Name => name;
        public string Photo => image!;
    }
}