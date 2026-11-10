using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

[Collection("SiteCurrency")]
public class PeriodBudgetTests
{
    private const string Json = """
    { "period": { "id": 7, "name": "Automne 2026", "periodKey": "2026-Q4" }, "currency": "EUR", "globalBudget": 500000,
      "lines": [
        { "category": "purchases", "label": "Stock", "planned": 300000, "actual": 150000, "remaining": 150000, "ratio": 0.5, "status": "ok", "details": [] },
        { "category": "trip", "label": "Voyage", "planned": 0, "actual": 12000, "remaining": -12000, "ratio": null, "status": "none",
          "details": [ { "key": "type:3", "label": "Hôtel", "actual": 12000 } ] },
        { "category": "advertising", "label": "Publicité", "planned": 10000, "actual": 13000, "remaining": -3000, "ratio": 1.3, "status": "over", "details": [] }
      ],
      "total": { "planned": 310000, "actual": 175000, "remaining": 135000, "ratio": 0.56, "status": "ok" } }
    """;

    private static PeriodBudget Read() => JsonConvert.DeserializeObject<PeriodBudget>(Json)!.WithCurrency();

    [Fact]
    public void Lines_ExposeStatusAndProgress()
    {
        var budget = Read();

        Assert.Equal(7, budget.period!.id);
        Assert.Equal(0.5, budget.lines[0].Progress);
        Assert.Equal("Dans le prévu", budget.lines[0].StatusText);
        Assert.Equal(0, budget.lines[1].Progress);
        Assert.False(budget.lines[1].HasPlanned);
        Assert.Equal("Aucun prévu", budget.lines[1].PlannedText);
        Assert.Equal(string.Empty, budget.lines[1].RemainingText);
        Assert.Equal(1, budget.lines[2].Progress);
        Assert.Equal("Dépassé", budget.lines[2].StatusText);
        Assert.Equal("Total", budget.total!.label);
        // Un poste sans prévu : le total ne se compare pas, sinon il paraîtrait dépassé.
        Assert.False(budget.TotalComparable);
        Assert.True(budget.ShowTotalHint);
    }

    [Fact]
    public void Amounts_UseBudgetCurrency_AndOverrunIsWrittenAsExceeded()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
            SiteCurrency.SetCurrency("CAD");
            var budget = Read();

            var overrun = budget.lines[2].RemainingText;
            Assert.StartsWith("Dépassé de", overrun);
            Assert.Contains("30", overrun);
            Assert.Contains("€", overrun);
            Assert.DoesNotContain("$", overrun);
            Assert.Contains("Hôtel", budget.lines[1].DetailsText);
            Assert.True(budget.HasGlobalBudget);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            SiteCurrency.Reset();
        }
    }

    [Fact]
    public async Task Service_UsesContractRoutesAndBodies()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new PeriodBudget() };
        var service = new PeriodBudgetService(fakeHttp);

        await service.GetAsync(7);
        await service.SavePlannedAsync(7, "trip", 25050);
        await service.CopyAsync(7);

        Assert.StartsWith("periodes/7/budget?locale=", Assert.Single(fakeHttp.RequestedUrls));
        var (putUrl, putBody) = Assert.Single(fakeHttp.PutData);
        Assert.StartsWith("periodes/7/budget?locale=", putUrl);
        Assert.Equal("{\"lines\":[{\"category\":\"trip\",\"planned\":25050}]}", JsonConvert.SerializeObject(putBody));
        var (copyUrl, copyBody) = Assert.Single(fakeHttp.PostedAuthData);
        Assert.StartsWith("periodes/7/budget/copier?locale=", copyUrl);
        Assert.Equal("{}", JsonConvert.SerializeObject(copyBody));
    }
}
