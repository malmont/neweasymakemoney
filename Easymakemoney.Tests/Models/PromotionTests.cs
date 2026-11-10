using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

[Collection("SiteCurrency")]
public class PromotionTests
{
    private const string Json = """
    { "rows": [
        { "productId": 15, "name": "Manteau", "regularPrice": 24900, "promoPrice": 19920, "marginRateRegular": 45.0, "marginRatePromo": 31.3,
          "stock": 12, "variantsOwnPrice": { "count": 1, "variants": [ { "variantId": 4, "variantLabel": "XL · Noir" } ] }, "changed": true, "alerts": ["VARIANT_OWN_PRICE"] },
        { "productId": 16, "name": "Parka", "regularPrice": 32900, "promoPrice": 13160, "changed": true, "alerts": ["BIG_CHANGE", "PROMOTION_EXISTS"] },
        { "productId": 8, "name": "Location kayak", "regularPrice": 2500, "promoPrice": 2500, "changed": false, "alerts": ["NOT_A_SALE_ITEM"] } ],
      "summary": { "products": 3, "changed": 2, "skipped": 1 },
      "period": { "startDate": "2026-11-12", "endDate": "2026-11-25", "days": 14 }, "previewHash": "h", "caveats": [] }
    """;

    [Fact]
    public void WritableCount_RespectsEachAgreement()
    {
        var preview = JsonConvert.DeserializeObject<PromotionPreview>(Json)!;

        Assert.Equal(1, preview.WritableCount(false, false, false));
        Assert.Equal(1, preview.WritableCount(false, true, false));
        Assert.Equal(2, preview.WritableCount(false, true, true));
        Assert.True(preview.HasBigChange && preview.HasExisting && !preview.HasBelowCost);
        Assert.Equal("Du 12/11/2026 au 25/11/2026 (14 jours). Le prix normal revient tout seul à la fin.", preview.PeriodText);
    }

    [Fact]
    public void Rows_ShowExcludedVariantsAndBlockingAlerts()
    {
        var rows = JsonConvert.DeserializeObject<PromotionPreview>(Json)!.rows;

        Assert.Equal("Pas de promotion sur 1 variante(s) à prix propre : XL · Noir", rows[0].VariantsText);
        Assert.False(rows[0].IsBlocking);
        Assert.True(rows[1].IsBlocking);
        Assert.Contains("remplacée", rows[1].AlertsText);
        Assert.Equal("Coût inconnu", rows[1].MarginText);
    }

    [Fact]
    public void Request_SendsStoreDaysAndOneScope()
    {
        var body = new PromotionRequest { AdviceId = 3, ProductIds = new List<int> { 1 }, Percent = 20, Rounding = "ends_99",
            StartDate = new DateTime(2026, 11, 12), EndDate = new DateTime(2026, 11, 25) }.ToBody();

        Assert.Equal(3, body["adviceId"]);
        Assert.False(body.ContainsKey("productIds"));
        Assert.Equal("2026-11-12", body["startDate"]);
        Assert.Equal("2026-11-25", body["endDate"]);
    }

    [Fact]
    public async Task Service_UsesContractRoutes()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new PromotionEndResult() };
        await new PromotionService(fakeHttp).EndAsync(5, onlyUntouched: true);
        fakeHttp.ResultToReturn = new List<PromotionItem>();
        await new PromotionService(fakeHttp).GetPromotionsAsync();

        Assert.Equal("gestion/promotions/terminer", fakeHttp.PostedAuthData[0].Url);
        Assert.Equal("{\"batchId\":5,\"onlyUntouched\":true}", JsonConvert.SerializeObject(fakeHttp.PostedAuthData[0].Data));
        Assert.Equal("gestion/promotions?status=all", Assert.Single(fakeHttp.RequestedUrls));
    }

    [Fact]
    public void Item_EndableOnlyWhenCreatedFromApp()
    {
        Assert.True(new PromotionItem { batchId = 2, status = "active" }.CanEnd);
        Assert.False(new PromotionItem { batchId = null, status = "active" }.CanEnd);
        Assert.True(new PromotionItem { batchId = null }.FromAdmin);
        Assert.True(new Advice { status = "new", kind = "SLOW_STOCK" }.CanCreatePromotion);
        Assert.False(new Advice { status = "new", kind = "FAST_SELLER" }.CanCreatePromotion);
    }
}
