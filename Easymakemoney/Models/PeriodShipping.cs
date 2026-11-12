namespace Easymakemoney.Models
{
    /// <summary>
    /// Lot B3 bis : GET /api/collections/{id}/frais-de-port, les frais de port de toutes les commandes d'une période en une lecture.
    /// Montants calculés par le serveur (priceConverted, totals.amount en cents de la devise du site).
    /// </summary>
    public class PeriodShipping
    {
        public string? currency { get; set; }
        public string? siteCurrency { get; set; }
        public List<PeriodShippingCommande> commandes { get; set; } = new();
        public PeriodShippingTotals? totals { get; set; }
    }

    public class PeriodShippingCommande
    {
        public int commandeId { get; set; }
        public string? name { get; set; }
        public string? currency { get; set; }
        public string? siteCurrency { get; set; }
        public FraisDePort? fraisDePort { get; set; }
    }

    public class PeriodShippingTotals
    {
        public int commandes { get; set; }
        public int withShipping { get; set; }
        public long amount { get; set; }
    }
}
