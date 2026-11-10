using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.UseCase.PeriodResultUseCase;
using Xunit;

namespace Easymakemoney.Tests.UseCases;

public class PeriodUseCaseTests
{
    private class FakePeriodService : IPeriodService
    {
        public int? CapturedPeriodId { get; private set; }
        public long? CapturedBudgetCents { get; private set; }
        public ListCollection? PeriodToReturn { get; set; }

        public Task<ListCollection?> GetCurrentPeriodAsync() =>
            Task.FromResult(PeriodToReturn);

        public Task UpdateBudgetAsync(int periodId, long budgetCents)
        {
            CapturedPeriodId = periodId;
            CapturedBudgetCents = budgetCents;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task UpdateBudgetAsync_RoundsAmountToCents_SendsToService()
    {
        var fakeService = new FakePeriodService();
        var useCase = new PeriodUseCase(fakeService);

        // Règle R3 : 12.345 envoie 1235
        await useCase.UpdateBudgetAsync(42, 12.345);

        Assert.Equal(42, fakeService.CapturedPeriodId);
        Assert.Equal(1235L, fakeService.CapturedBudgetCents);
    }

    [Fact]
    public async Task GetCurrentPeriodAsync_RelaysToPeriodService()
    {
        var expected = new ListCollection
        {
            id = 7,
            nomCollection = "Hiver 2026",
            photoCollection = "photo.png",
            startDateCollection = "2026-12-01T00:00:00Z",
            endDateCollection = "2027-02-28T00:00:00Z"
        };
        var fakeService = new FakePeriodService { PeriodToReturn = expected };
        var useCase = new PeriodUseCase(fakeService);

        var result = await useCase.GetCurrentPeriodAsync();

        Assert.Same(expected, result);
    }
}

