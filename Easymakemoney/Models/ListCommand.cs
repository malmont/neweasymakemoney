namespace Easymakemoney.Models
{
    public class ListCommand : IListItem
    {
        public int id { get; set; }
        public required string name { get; set; }
        public required string photo { get; set; }
        public double? budget { get; set; }
        public required string date { get; set; }
        public int collectionId { get; set; }
        public ListFournisseur? fournisseur { get; set; }

        // Implémentation de l'interface IListItem
        public int Id => id;
        public string Name => name;
        public string Photo => photo;
    }
}
