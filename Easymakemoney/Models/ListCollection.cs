namespace Easymakemoney.Models
{
    public class ListCollection : IListItem
    {
        public int id { get; set; }
        public double budgetCollection { get; set; }
        public required string startDateCollection { get; set; }
        public required string endDateCollection { get; set; }
        public bool del { get; set; }
        public required string nomCollection { get; set; }
        public required string photoCollection { get; set; }
        public int userId { get; set; }

        // Implémentation de l'interface IListItem
        public int Id => id;
        public string Name => nomCollection;
        public string Photo => photoCollection;
    }
}
