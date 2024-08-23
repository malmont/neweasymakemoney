namespace Easymakemoney.Models.DashboardCommand
{
    public class DashboardCommand
    {
        public double averageMultiplier { get; set; }
        public required BudgetGeneral budgetGeneral { get; set; }

        public double totalItemCost { get; set; }

        public double totalFraisDePort { get; set; }

        public required Statistics statistics { get; set; }

         public required ValeurStock valeurStock { get; set; }

        public required TauxMarge tauxMarge { get; set; }

        public required Transporteur transporteur { get; set; }

    }
}