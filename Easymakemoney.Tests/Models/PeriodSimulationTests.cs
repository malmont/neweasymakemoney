using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

[Collection("SiteCurrency")]
public class PeriodSimulationTests
{
    private const string Json = """
    { "period": { "id": 7, "name": "Automne 2026", "daysTotal": 92, "daysElapsed": 41, "daysRemaining": 51 }, "currency": "CAD",
      "method": "pace", "lastYear": null,
      "base": { "sales": { "realized": 100000, "projected": 50000, "total": 150000 }, "grossMargin": 60000, "tauxMarque": 40.0,
                "expenses": { "byCategory": [], "total": 70000 }, "result": -10000, "breakEven": { "revenue": 175000, "reached": false },
                "stockEnd": { "units": 10, "costValue": 20000, "saleValue": 45000 }, "cashAfterPurchases": null },
      "simulated": { "sales": { "realized": 100000, "projected": 55000, "total": 155000 }, "grossMargin": 65000, "tauxMarque": 41.94,
                "expenses": { "byCategory": [], "total": 70000 }, "result": -5000, "breakEven": null,
                "stockEnd": { "units": 10, "costValue": 20000, "saleValue": 49500 }, "cashAfterPurchases": null },
      "delta": { "sales": { "realized": 0, "projected": 5000, "total": 5000 } },
      "plannedToSave": { "lines": [ { "category": "advertising", "planned": 10000 } ] },
      "caveats": [ { "code": "SAME_QUANTITIES", "text": "À quantités vendues égales." } ] }
    """;

    [Fact]
    public void Rows_CompareBaseAndSimulated_WithToneByMeaning()
    {
        var simulation = JsonConvert.DeserializeObject<PeriodSimulation>(Json)!;
        var rows = simulation.Rows();

        var result = rows.Single(row => row.Label == "Résultat");
        Assert.True(result.IsTotal);
        Assert.Equal(1, result.Tone); // résultat qui monte : mieux
        Assert.StartsWith("+", result.DeltaText);

        var rate = rows.Single(row => row.Label == "Taux de marque");
        Assert.Contains("pt", rate.DeltaText);

        var expenses = rows.Single(row => row.Label == "Frais de la période");
        Assert.False(expenses.HasDelta); // frais inchangés

        var breakEven = rows.Single(row => row.Label.StartsWith("Point mort"));
        Assert.Equal("—", breakEven.SimulatedText); // point mort nul : marge insuffisante
        Assert.False(breakEven.HasDelta);

        var cash = rows.Single(row => row.Label == "Trésorerie après achats");
        Assert.Equal("—", cash.BaseText);

        Assert.Equal("Automne 2026 · 51 jours restants sur 92", simulation.PeriodText);
        Assert.Contains("30 derniers jours", simulation.MethodText);
    }

    [Fact]
    public async Task Service_SendsOnlyLeversThatAreSet()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new PeriodSimulation() };
        var service = new SimulationService(fakeHttp);

        await service.SimulateAsync(7, new SimulationLevers { pricePercent = 5, promotion = new SimulationPromotion { percent = 10 } });

        var (url, body) = Assert.Single(fakeHttp.PostedAuthData);
        Assert.Equal("periodes/7/simulation?locale=fr", url);
        Assert.Equal("{\"pricePercent\":5.0,\"promotion\":{\"percent\":10.0,\"scope\":\"all\"}}", JsonConvert.SerializeObject(body));
    }
}
