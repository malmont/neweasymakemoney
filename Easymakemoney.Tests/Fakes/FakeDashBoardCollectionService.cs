using Easymakemoney.Models.DashbordCollection;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Faux service du tableau de bord d'une période ou collection.</summary>
public class FakeDashBoardCollectionService : IDashBoardCollectionService
{
    public List<int> RequestedIds { get; } = new();
    public DashBoardCollection? ResultToReturn { get; set; }
    public Exception? ExceptionToThrow { get; set; }

    public static DashBoardCollection CreateSample(double totalItemCost = 1200) => new()
    {
        averageMultiplier = 2.0,
        budgetGeneral = new BudgetGeneral { generalBudget = 5000, usedBudget = 1200, remainingBudget = 3800 },
        collectionDuration = new CollectionDuration { startDate = "2026-11-01", endDate = "2026-11-30", days = "30" },
        totalItemCost = totalItemCost,
        generalExpenses = new GeneralExpenses { totalExpenseCost = 100, totalShippingCost = 50, totalGeneralExpenses = 150 },
        statistics = new Statistics { orderCount = 3, itemCount = 15, modelCount = 5 },
        valeurStock = new ValeurStock { stockValue = 2400, marge = 1200 },
        tauxMarge = new TauxMarge { tauxMarge = 50, tauxMarque = 100 }
    };

    public Task<DashBoardCollection> GetDashBoardCollection(int collectionId)
    {
        RequestedIds.Add(collectionId);
        if (ExceptionToThrow != null)
            throw ExceptionToThrow;
        return Task.FromResult(ResultToReturn ?? CreateSample());
    }
}

