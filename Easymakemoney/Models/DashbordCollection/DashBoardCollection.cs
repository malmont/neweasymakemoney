namespace Easymakemoney.Models.DashbordCollection
{
    public class DashBoardCollection
    {
        public double averageMultiplier { get; set; }
        public required BudgetGeneral budgetGeneral { get; set; }
        public required CollectionDuration collectionDuration { get; set; }
        public double totalItemCost { get; set; }
        public required GeneralExpenses generalExpenses { get; set; }
        public required Statistics statistics { get; set; }
        public required ValeurStock valeurStock { get; set; }

        public required TauxMarge tauxMarge { get; set; }

    }
}