
namespace Easymakemoney.Models
{
    public class ListCollection
    {
        public int id { get; set; }
        public double budgetCollection { get; set; }
        public required string startDateCollection { get; set; }
        public required string endDateCollection { get; set; }
        public bool del { get; set; }
        public required string nomCollection { get; set; }
        public required string photoCollection { get; set; }
        public int userId { get; set; }
    }
}

