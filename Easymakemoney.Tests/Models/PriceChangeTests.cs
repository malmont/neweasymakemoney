using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

[Collection("SiteCurrency")]
public class PriceChangeTests
{
    private const string Json = """
    { "rows": [
        { "productId": 1, "variantId": null, "level": "product", "name": "Robe", "inheritingVariants": 3, "oldPrice": 4000, "newPrice": 4199,
          "cost": 2000, "marginRateBefore": 50.0, "marginRateAfter": 52.4, "stock": 4, "changed": true, "alerts": [] },
        { "productId": 2, "variantId": 9, "level": "variant", "name": "Chemise XL", "oldPrice": 1000, "newPrice": 900,
          "cost": 950, "marginRateBefore": 5.0, "marginRateAfter": -5.6, "stock": 1, "changed": true, "alerts": ["BELOW_COST"] },
        { "productId": 3, "variantId": null, "level": "product", "name": "Manteau", "oldPrice": 10000, "newPrice": 16000,
          "cost": 5000, "marginRateBefore": 50.0, "marginRateAfter": 68.8, "stock": 2, "changed": true, "alerts": ["BIG_CHANGE", "SPECIAL_PRICE_ACTIVE"] },
        { "productId": 4, "variantId": null, "level": "product", "name": "Location", "oldPrice": 5000, "newPrice": 5000,
          "cost": null, "stock": 0, "changed": false, "alerts": ["NOT_A_SALE_ITEM"] } ],
      "summary": { "products": 4, "rows": 4, "changed": 3, "skipped": 1 },
      "pricesIncludeTax": true, "previewHash": "abc",
      "caveats": [ { "code": "PRICES_INCLUDE_TAX", "text": "La marge comprend la taxe." } ] }
    """;

    [Fact]
    public void WritableCount_FollowsBackendRules()
    {
        var preview = JsonConvert.DeserializeObject<PricePreview>(Json)!;

        Assert.True(preview.HasBelowCost);
        Assert.True(preview.HasBigChange);
        Assert.Equal(1, preview.WritableCount(false, false));
        Assert.Equal(2, preview.WritableCount(true, false));
        Assert.Equal(3, preview.WritableCount(true, true));
    }

    [Fact]
    public void Rows_DescribeVariantsAlertsAndMargins()
    {
        var rows = JsonConvert.DeserializeObject<PricePreview>(Json)!.rows;

        Assert.Equal("S'applique aussi à 3 variante(s) sans prix propre", rows[0].VariantsText);
        Assert.Contains("→", rows[0].PriceText);
        Assert.Equal("Chemise XL (variante)", rows[1].Title);
        rows[1].variantLabel = "XL · Noir";
        Assert.Equal("Chemise XL · XL · Noir", rows[1].Title);
        Assert.True(rows[1].IsBlocking);
        Assert.Equal("Écart de plus de 50 % · Promotion en cours, son prix reste inchangé", rows[2].AlertsText);
        Assert.False(rows[3].changed);
        Assert.Equal("Coût inconnu", rows[3].MarginText);
    }

    [Fact]
    public void Request_SendsExactlyOneScope()
    {
        var fromAdvice = new PriceChangeRequest { AdviceId = 7, CommandeId = 3, Mode = "percent", Value = 5, Rounding = "ends_99" }.ToBody();
        Assert.Equal(7, fromAdvice["adviceId"]);
        Assert.False(fromAdvice.ContainsKey("commandeId"));

        var fromCommande = new PriceChangeRequest { CommandeId = 3, Mode = "coefficient", Value = 2.2 }.ToBody();
        Assert.Equal(3, fromCommande["commandeId"]);
        Assert.Equal("none", fromCommande["rounding"]);
    }

    [Fact]
    public async Task Service_UsesContractRoutes()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new PriceApplyResult() };
        var service = new PriceService(fakeHttp);
        var request = new PriceChangeRequest { CommandeId = 3, Mode = "coefficient", Value = 2 };

        await service.ApplyAsync(request, "abc", allowBelowCost: false, allowBigChange: true);
        fakeHttp.ResultToReturn = new PriceUndoResult();
        await service.UndoAsync(12, onlyUntouched: true);

        Assert.Equal("gestion/prix/appliquer", fakeHttp.PostedAuthData[0].Url);
        var body = Assert.IsType<Dictionary<string, object>>(fakeHttp.PostedAuthData[0].Data);
        Assert.Equal("abc", body["previewHash"]);
        Assert.Equal(true, body["allowBigChange"]);
        Assert.Equal("gestion/prix/annuler", fakeHttp.PostedAuthData[1].Url);
        Assert.Equal("{\"batchId\":12,\"onlyUntouched\":true}", JsonConvert.SerializeObject(fakeHttp.PostedAuthData[1].Data));
    }

    [Fact]
    public void Batch_StatusAndTitle()
    {
        var batch = JsonConvert.DeserializeObject<PriceBatch>("""
        { "id": 12, "mode": "percent", "value": 5, "changed": 3, "createdBy": { "name": "Gérant" }, "createdAt": "2026-11-10T20:00:00+00:00",
          "undoneAt": null, "undoable": false, "undoBlockedBy": "PRICE_EDITED_SINCE" }
        """)!;

        Assert.Equal("Hausse de 5 %", batch.Title);
        Assert.Contains("Gérant", batch.DetailText);
        Assert.Equal("Des prix ont été retouchés depuis", batch.StatusText);
        Assert.False(batch.CanUndo);
    }

    [Fact]
    public async Task PriceService_PreviewAsync_SendsPostWithSingleScope()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new PricePreview() };
        var service = new PriceService(fakeHttp);
        var request = new PriceChangeRequest { CommandeId = 3, Mode = "percent", Value = 10, Rounding = "ends_99" };

        await service.PreviewAsync(request);

        var (url, data) = Assert.Single(fakeHttp.PostedAuthData);
        Assert.Equal("gestion/prix/apercu", url);
        var body = Assert.IsType<Dictionary<string, object>>(data);
        Assert.Equal(3, body["commandeId"]);
        Assert.False(body.ContainsKey("adviceId"));
        Assert.False(body.ContainsKey("productIds"));
        Assert.Equal("percent", body["mode"]);
        Assert.Equal(10.0, body["value"]);
        Assert.Equal("ends_99", body["rounding"]);
    }
}
