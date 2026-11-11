using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.Models;

[Collection("SiteCurrency")]
public class PreparationTests
{
    [Fact]
    public void Cents_ReadsNumberOrObject()
    {
        Assert.Equal(1500, PreparationValues.Cents(JToken.Parse("1500")));
        Assert.Equal(2000, PreparationValues.Cents(JToken.Parse("""{ "amount": 2000, "date": "2026-12-01" }""")));
        Assert.Equal(300, PreparationValues.Cents(JToken.Parse("""{ "revenue": 300, "reached": false }"""), "revenue"));
        Assert.Null(PreparationValues.Cents(JValue.CreateNull()));
        Assert.Null(PreparationValues.Cents(null));
    }

    [Fact]
    public void Simulation_RowsHaveDefinitions_AndPlannedTextShowsModes()
    {
        var simulation = JsonConvert.DeserializeObject<PreparationSimulation>("""
        { "currency": "CAD",
          "plan": { "sales": { "target": 500000, "planned": 450000, "capped": true }, "grossMargin": 200000, "tauxMarque": 44.4,
                    "expenses": { "total": 120000, "tripInOtherPeriod": true }, "result": 80000, "breakEven": { "revenue": 270270 },
                    "purchases": { "total": 300000 }, "stockEnd": { "saleValue": 900000 }, "cash": { "low": { "date": "2026-10-20", "amount": -5000 } } },
          "planned": [ { "period": { "id": 8, "name": "Automne 2026" }, "purchases": 300000, "trip": 150000, "mode": "add" },
                       { "period": { "id": 9, "name": "Hiver 2026-2027" }, "purchases": 0, "shipping": 20000, "mode": "replace" } ],
          "previewHash": "h", "caveats": [] }
        """)!;

        var rows = simulation.Rows();
        Assert.All(rows, row => Assert.False(string.IsNullOrWhiteSpace(row.Help)));
        Assert.Contains("4", rows.Single(r => r.Label == "Ventes prévues").BaseText);
        Assert.True(simulation.plan.SalesCapped);
        Assert.True(simulation.plan.TripInOtherPeriod);
        Assert.Equal("le 20/10/2026", simulation.CashLowDateText);
        Assert.Contains("Automne 2026 (ajouté au budget existant)", simulation.PlannedText);
        Assert.Contains("Hiver 2026-2027 (remplace le budget)", simulation.PlannedText);
    }

    [Fact]
    public void Request_BodyFollowsContract()
    {
        var body = new PreparationRequest
        {
            Date = "2026-12-01",
            SalesMethod = "custom",
            SalesAmount = 500000,
            Purchases = { new PreparationPurchase { Date = new DateTime(2026, 10, 20), AmountCents = 300000, Coefficient = 2.2 } },
            Trip = new PreparationTrip { StartDate = new DateTime(2026, 10, 18), EndDate = new DateTime(2026, 10, 22), Place = " Paris ", BudgetTotalCents = 150000 },
            Shipping = 20000,
        }.ToBody();

        Assert.Equal(
            "{\"date\":\"2026-12-01\",\"sales\":{\"method\":\"custom\",\"amount\":500000},\"purchases\":[{\"date\":\"2026-10-20\",\"amount\":300000,\"coefficient\":2.2}],\"locale\":\"fr\",\"trip\":{\"startDate\":\"2026-10-18\",\"endDate\":\"2026-10-22\",\"budgetTotal\":150000,\"place\":\"Paris\"},\"planned\":{\"shipping\":20000}}",
            JsonConvert.SerializeObject(body));
    }

    [Fact]
    public async Task Service_UsesContractRoutes()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new PreparationEstimates() };
        var service = new PreparationService(fakeHttp);

        await service.GetEstimatesAsync("2026-12-01");
        await service.ValidateAsync(new PreparationRequest { Date = "2026-12-01" }, "hash");

        Assert.Equal("gestion/preparation/estimations?date=2026-12-01", Assert.Single(fakeHttp.RequestedUrls));
        var (url, data) = Assert.Single(fakeHttp.PostedAuthData);
        Assert.Equal("gestion/preparation/valider", url);
        Assert.Equal("hash", Assert.IsType<Dictionary<string, object>>(data)["previewHash"]);
    }
}
