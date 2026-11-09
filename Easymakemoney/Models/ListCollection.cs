namespace Easymakemoney.Models
{
    public class ListCollection : IListItem
    {
        public int id { get; set; }
        public long? budgetCollection { get; set; }
        public required string startDateCollection { get; set; }
        public required string endDateCollection { get; set; }
        public bool del { get; set; }
        public bool IsClosed { get; set; } = false;

        public required string nomCollection { get; set; }
        public required string photoCollection { get; set; }
        public int userId { get; set; }

        public string? kind { get; set; }
        public string? periodKey { get; set; }
        public string? status { get; set; }
        public string? closedAt { get; set; }
        public string? reopenedAt { get; set; }

        public bool IsPeriod => kind == "period";
        public bool CanDelete => !IsPeriod;

        public string StatusText
        {
            get
            {
                if (IsClosed)
                    return "Clôturée";

                return DateTimeOffset.TryParse(startDateCollection, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var start)
                    && start > DateTimeOffset.Now ? "À venir" : "En cours";
            }
        }

        // Implémentation de l'interface IListItem
        public int Id => id;
        public string Name => nomCollection;
        public string Photo => photoCollection;
    }
}
