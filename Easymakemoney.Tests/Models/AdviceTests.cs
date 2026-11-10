using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

[Collection("SiteCurrency")]
public class AdviceTests
{
    // Exemple réel transmis par le backend (demo, règles seules), raccourci.
    private const string Json = """
    { "period": { "id": 8, "name": "Automne 2026", "start": "2026-09-01", "end": "2026-11-30" },
      "moments": ["selling"], "currency": "CAD", "headline": null,
      "ai": { "status": "rules_only", "generatedAt": null }, "generating": false, "manualRefreshesLeft": 3,
      "advices": [{
        "id": 1, "periodId": 8, "moment": "selling", "kind": "SLOW_STOCK",
        "title": "Manteau de laine doublé dort en stock",
        "text": "Aucune vente depuis 335 jours, 1 643,40 $ immobilisés au coût. Essayez une promotion de 20 % dans le simulateur.",
        "textSource": "rules",
        "action": { "screen": "simulateur", "params": { "periodId": 8, "promotion": { "percent": 20, "scope": "products", "productIds": [15] } } },
        "effect": { "amount": 164340, "label": "Argent immobilisé" }, "confidence": "low",
        "sources": [ { "label": "Stock actuel", "value": 12, "unit": "count" }, { "label": "Valeur au coût", "value": 164340, "unit": "money" },
                     { "label": "Jours sans vente", "value": 335, "unit": "days" }, { "label": "Promotion proposée", "value": 20, "unit": "percent" } ],
        "figures": [ { "productId": 15, "variantId": null, "name": "Manteau de laine doublé", "price": 24900, "cost": 13695, "stock": 12, "soldLast30Days": 0 } ],
        "subject": { "productIds": [15] }, "status": "new", "active": true, "rank": 1 }] }
    """;

    [Fact]
    public void List_ReadsRealExample_AndDescribesRulesOnlyMode()
    {
        var list = JsonConvert.DeserializeObject<AdviceList>(Json)!.WithCurrency();

        Assert.Equal("Pendant la vente", list.MomentsText);
        Assert.False(list.HasHeadline);
        Assert.Equal("Conseils calculés sur vos chiffres.", list.AiText);
        Assert.True(list.CanRefresh);

        var advice = Assert.Single(list.advices);
        Assert.Equal("CAD", advice.Currency);
        Assert.Equal("Essayer dans le simulateur", advice.ActionText);
        Assert.Equal("Estimation prudente", advice.ConfidenceText);
        Assert.False(advice.IsRisk);
        Assert.False(advice.WrittenByAi);
    }

    [Fact]
    public void Sources_AreFormattedByUnit()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
            var advice = JsonConvert.DeserializeObject<AdviceList>(Json)!.WithCurrency().advices[0];

            var lines = advice.SourcesText.Split('\n');
            Assert.Equal("Stock actuel : 12", lines[0]);
            Assert.Contains("1", lines[1]);
            Assert.Contains("643,40", lines[1]);
            Assert.Contains("$", lines[1]);
            Assert.Equal("Jours sans vente : 335 jours", lines[2]);
            Assert.Equal("Promotion proposée : 20 %", lines[3]);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            SiteCurrency.Reset();
        }
    }

    [Fact]
    public void Figures_BecomeArticlesWithPriceAndNoDaysLeft()
    {
        var advice = JsonConvert.DeserializeObject<AdviceList>(Json)!.advices[0];
        var item = Assert.Single(advice.figures!.ToObject<List<LowStockItem>>()!);

        Assert.Equal(24900, item.price);
        Assert.False(item.HasDaysLeft); // un article qui dort n'a pas de rythme de vente
        Assert.True(item.HasPrice);
    }

    [Fact]
    public async Task Service_UsesContractRoutes()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new AdviceList() };
        var service = new AdviceService(fakeHttp);

        await service.GetAsync();
        await service.DoneAsync(3);
        await service.DismissAsync(4);
        await service.RefreshAsync(8);

        Assert.Equal("gestion/conseils?status=new&locale=fr", Assert.Single(fakeHttp.RequestedUrls));
        Assert.Equal(new[] { "gestion/conseils/3/fait", "gestion/conseils/4/ecarter", "gestion/conseils/actualiser" },
            fakeHttp.PostedAuthData.Select(post => post.Url));
        Assert.Equal("{\"periodId\":8}", JsonConvert.SerializeObject(fakeHttp.PostedAuthData[2].Data));
    }

    [Fact]
    public void SimulationPromotion_SendsProductsOnlyWhenScoped()
    {
        Assert.Equal("{\"percent\":20.0,\"scope\":\"products\",\"productIds\":[15]}",
            JsonConvert.SerializeObject(new SimulationPromotion { percent = 20, scope = "products", productIds = new List<int> { 15 } }));
        Assert.Equal("{\"percent\":10.0,\"scope\":\"all\"}", JsonConvert.SerializeObject(new SimulationPromotion { percent = 10 }));
    }
}
