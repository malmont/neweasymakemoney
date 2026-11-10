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

[Collection("SiteCurrency")]
public class SimulationCommentTests
{
    [Fact]
    public void Comment_MarksRecommendedCandidate_AndDescribesPlanned()
    {
        var comment = JsonConvert.DeserializeObject<SimulationComment>("""
        { "simulation": { "currency": "CAD" },
          "candidates": [
            { "key": "keep", "label": "Garder le prévu", "planned": { "purchases": 300000, "trip": 0, "shipping": 20000, "advertising": 10000, "other": 5000 }, "result": -23405, "cashAfterPurchases": -27000 },
            { "key": "cashSafe", "label": "Trésorerie sûre", "planned": { "purchases": 273000, "trip": 0, "shipping": 20000, "advertising": 10000, "other": 5000 }, "result": -23405, "cashAfterPurchases": 0, "purchasesCut": 27000 } ],
          "comment": { "recommended": "cashSafe", "title": "Réduire un peu les achats", "text": "…", "textSource": "rules" },
          "ai": { "status": "not_included" }, "cached": false }
        """)!.Prepare();

        Assert.False(comment.candidates[0].IsRecommended);
        Assert.True(comment.candidates[1].IsRecommended);
        Assert.Equal("CAD", comment.candidates[1].Currency);
        Assert.StartsWith("Stock ", comment.candidates[1].PlannedText);
        Assert.Contains("Publicité", comment.candidates[1].PlannedText);
        Assert.Equal("Avis calculé sur vos chiffres.", comment.SourceText);
    }

    [Fact]
    public async Task Services_UseCommentRouteAndPutAllLines()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new SimulationComment() };
        await new SimulationService(fakeHttp).CommentAsync(7, new SimulationLevers());
        Assert.Equal("periodes/7/simulation/commentaire?locale=fr", Assert.Single(fakeHttp.PostedAuthData).Url);

        fakeHttp.ResultToReturn = new PeriodBudget();
        await new PeriodBudgetService(fakeHttp).SaveLinesAsync(7, new Dictionary<string, long> { ["purchases"] = 273000, ["other"] = 5000 });
        Assert.Equal("{\"lines\":[{\"category\":\"purchases\",\"planned\":273000},{\"category\":\"other\",\"planned\":5000}]}",
            JsonConvert.SerializeObject(Assert.Single(fakeHttp.PutData).Data));
    }
}

public class SimulationHelpTests
{
    [Fact]
    public void EveryRow_HasADefinition()
    {
        var rows = new PeriodSimulation().Rows();

        Assert.Equal(9, rows.Count);
        Assert.All(rows, row => Assert.False(string.IsNullOrWhiteSpace(row.Help), row.Label));
        Assert.Contains("frais ÷ taux de marque", rows.Single(row => row.Label.StartsWith("Point mort")).Help);
    }
}
