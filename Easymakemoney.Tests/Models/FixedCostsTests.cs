using Easymakemoney.Models;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class FixedCostsTests
{
    private static TreasuryMovement Movement(string label, string category, string recurrence, long amount, string direction = "out", string? next = "2026-12-01") =>
        new() { id = 1, label = label, category = category, recurrence = recurrence, amount = amount, direction = direction, nextDate = next };

    [Fact]
    public void Select_KeepsOnlyRunningRecurringOutflows_AndExcludesLoans()
    {
        var movements = new[]
        {
            Movement("Loyer", "rent", "monthly", 120000),
            Movement("Assurance", "insurance", "yearly", 240000),
            Movement("Prêt", "loan", "monthly", 50000),
            Movement("Remboursement de taxe", "tax", "monthly", 10000, direction: "in"),
            Movement("Achat ponctuel", "other", "none", 9000),
            Movement("Ancien abonnement", "subscription", "monthly", 3000, next: null),
        };

        var fixedCosts = FixedCosts.Select(movements).ToList();

        Assert.Equal(new[] { "Loyer", "Assurance" }, fixedCosts.Select(m => m.label));
    }

    [Theory]
    [InlineData("monthly", 120000, 120000)]
    [InlineData("yearly", 240000, 20000)]
    [InlineData("quarterly", 30000, 10000)]
    [InlineData("weekly", 12000, 52000)]
    public void MonthlyEquivalent_FollowsRecurrence(string recurrence, long amount, long expected)
    {
        Assert.Equal(expected, FixedCosts.MonthlyEquivalent(Movement("x", "rent", recurrence, amount)));
    }

    [Fact]
    public void MonthlyTotal_SumsEquivalents()
    {
        var items = new[] { Movement("Loyer", "rent", "monthly", 120000), Movement("Assurance", "insurance", "yearly", 240000) };
        Assert.Equal(140000, FixedCosts.MonthlyTotal(items));
    }
}
