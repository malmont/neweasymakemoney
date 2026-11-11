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

    [Fact]
    public async Task PromotionService_PreviewAsync_SendsPostWithSingleScopeAndIsoDates()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new PromotionPreview() };
        var service = new PromotionService(fakeHttp);
        var request = new PromotionRequest
        {
            AdviceId = 5,
            Percent = 25,
            Rounding = "none",
            StartDate = new DateTime(2026, 11, 15),
            EndDate = new DateTime(2026, 11, 29)
        };

        await service.PreviewAsync(request);

        var (url, data) = Assert.Single(fakeHttp.PostedAuthData);
        Assert.Equal("gestion/promotions/apercu", url);
        var body = Assert.IsType<Dictionary<string, object>>(data);
        Assert.Equal(5, body["adviceId"]);
        Assert.False(body.ContainsKey("commandeId"));
        Assert.False(body.ContainsKey("productIds"));
        Assert.Equal("2026-11-15", body["startDate"]);
        Assert.Equal("2026-11-29", body["endDate"]);
        Assert.Equal(25.0, body["percent"]);
    }

    [Fact]
    public async Task PromotionService_ApplyAsync_SendsPreviewHashAndThreeAgreements()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new PriceApplyResult() };
        var service = new PromotionService(fakeHttp);
        var request = new PromotionRequest
        {
            ProductIds = new List<int> { 12, 14 },
            Percent = 20,
            Rounding = "ends_99",
            StartDate = new DateTime(2026, 11, 12),
            EndDate = new DateTime(2026, 11, 25)
        };

        await service.ApplyAsync(request, previewHash: "hash-promo-123", allowBelowCost: true, allowBigChange: false, allowReplace: true);

        var (url, data) = Assert.Single(fakeHttp.PostedAuthData);
        Assert.Equal("gestion/promotions/appliquer", url);
        var body = Assert.IsType<Dictionary<string, object>>(data);
        Assert.Equal("hash-promo-123", body["previewHash"]);
        Assert.Equal(true, body["allowBelowCost"]);
        Assert.Equal(false, body["allowBigChange"]);
        Assert.Equal(true, body["allowReplace"]);
        Assert.Equal(new List<int> { 12, 14 }, body["productIds"]);
    }

    [Fact]
    public void PromotionItem_DatesTextIn_UsesExplicitTimeZoneToShiftIsoDate()
    {
        var toronto = TimeZoneInfo.FindSystemTimeZoneById("America/Toronto");

        var itemSameDay = new PromotionItem
        {
            startsAt = "2026-10-01T04:00:00+00:00",
            endsAt = "2026-10-15T04:00:00+00:00"
        };
        Assert.Equal("Du 01/10/2026 au 15/10/2026", itemSameDay.DatesTextIn(toronto));

        var itemPreviousDay = new PromotionItem
        {
            startsAt = "2026-10-01T03:00:00+00:00",
            endsAt = "2026-10-15T03:00:00+00:00"
        };
        Assert.Equal("Du 30/09/2026 au 14/10/2026", itemPreviousDay.DatesTextIn(toronto));
    }
}
