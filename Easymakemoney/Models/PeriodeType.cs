namespace Easymakemoney.Models
{
    public class PeriodeType : IListItem
    {
        public int id { get; set; }
        public required string typePeriode { get; set; }
        public required string photoPeriode { get; set; }

        // Implémentation de l'interface IListItem
        public int Id => id;
        public string Name => typePeriode;
        public string Photo => photoPeriode;
    }
}
