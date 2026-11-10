using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Xunit;

namespace Easymakemoney.Tests.Services;

public class PeriodServiceTests
{
    [Fact]
    public async Task GetCurrentPeriodAsync_CallsPeriodesCouranteRoute()
    {
        var expectedPeriod = new ListCollection
        {
            id = 5,
            nomCollection = "Automne 2026",
            photoCollection = "photo.png",
            startDateCollection = "2026-09-01T00:00:00Z",
            endDateCollection = "2026-11-30T00:00:00Z",
            kind = "period"
        };

        var fakeHttp = new FakeHttpService
        {
            ResultToReturn = expectedPeriod
        };
        var service = new PeriodService(fakeHttp);

        var result = await service.GetCurrentPeriodAsync();

        Assert.NotNull(result);
        Assert.Equal(5, result.id);
        Assert.Equal("Automne 2026", result.nomCollection);
        Assert.Single(fakeHttp.RequestedUrls);
        Assert.Equal("periodes/courante", fakeHttp.RequestedUrls[0]);
    }

    [Fact]
    public async Task UpdateBudgetAsync_SendsPatchWithBudgetCents()
    {
        var fakeHttp = new FakeHttpService();
        var service = new PeriodService(fakeHttp);

        await service.UpdateBudgetAsync(12, 150000L);

        Assert.Single(fakeHttp.PostedAuthData);
        var (url, bodyObj) = fakeHttp.PostedAuthData[0];
        Assert.Equal("periodes/12", url);

        var prop = bodyObj.GetType().GetProperty("budget");
        Assert.NotNull(prop);
        Assert.Equal(150000L, prop.GetValue(bodyObj));
    }
}

