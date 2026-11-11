using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Promotion par variante (lot backend VAR, promotions-gestion.md § 8).</summary>
[Collection("SiteCurrency")]
public class VariantPromotionTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public VariantPromotionTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
        VariantPromotionListViewModel.PromotionsChanged = false;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
        VariantPromotionListViewModel.PromotionsChanged = false;
    }

    private const string PreviewJson = """
    { "rows": [
        { "variantId": 2, "productId": 2, "name": "T-shirt Horizon", "variantLabel": "XL · Noir", "basePrice": 3900, "promoPrice": 3120, "discount": 780,
          "marginRateBase": 48.7, "marginRatePromo": 35.9, "stock": 498, "changed": true, "alerts": [ "PRODUCT_PROMOTION_ACTIVE" ] },
        { "variantId": 3, "productId": 2, "name": "T-shirt Horizon", "variantLabel": "M · Blanc", "basePrice": 3500, "promoPrice": 1200, "changed": true, "alerts": [ "BELOW_COST", "BIG_CHANGE" ] },
        { "variantId": 4, "productId": 2, "name": "T-shirt Horizon", "variantLabel": "S · Blanc", "basePrice": 3900, "promoPrice": 3500, "changed": true, "alerts": [ "VARIANT_PRICE_ABOVE_PRODUCT_PROMOTION" ] },
        { "variantId": 5, "productId": 2, "name": "T-shirt Horizon", "variantLabel": "Sans prix", "changed": false, "alerts": [ "PRICE_MISSING" ] } ],
      "summary": { "variants": 4, "changed": 3, "skipped": 1 }, "period": { "startDate": "2026-11-12", "endDate": "2026-11-21", "days": 10 },
      "enabled": false, "warnings": [ "VARIANT_PROMOTIONS_OFF" ], "pricesIncludeTax": false, "previewHash": "h", "caveats": [] }
    """;

    [Fact]
    public void Preview_WritableCount_FollowsAllowFlags()
    {
        var preview = JsonConvert.DeserializeObject<VariantPromotionPreview>(PreviewJson)!;

        Assert.True(preview.IsOff);
        Assert.Equal(1, preview.WritableCount(false, false, false, false));
        Assert.Equal(1, preview.WritableCount(true, false, false, false)); // BIG_CHANGE bloque encore
        Assert.Equal(2, preview.WritableCount(true, true, false, false));
        Assert.Equal(3, preview.WritableCount(true, true, false, true));
        Assert.True(preview.rows[3].IsBlocked);
        Assert.Equal("Du 12/11/2026 au 21/11/2026 (10 jours)", preview.PeriodText);
    }

    [Fact]
    public void Row_Texts()
    {
        var row = JsonConvert.DeserializeObject<VariantPromotionPreview>(PreviewJson)!.rows[0];
        row.Currency = "CAD";

        Assert.Equal("XL · Noir", row.Label);
        Assert.Contains("→", row.PriceText);
        Assert.Equal("Marge 48,7 % → 35,9 %", row.MarginText);
        Assert.Equal("Remplace la promotion de l'article pendant ces dates", row.AlertsText);
    }

    [Fact]
    public void Request_Bodies()
    {
        var byProduct = new VariantPromotionRequest { ProductIds = new() { 2 }, Percent = 12.5, Rounding = "ends_99", StartDate = new DateTime(2026, 11, 12), EndDate = new DateTime(2026, 11, 21) };
        Assert.Equal("""{"percent":12.5,"rounding":"ends_99","startDate":"2026-11-12","endDate":"2026-11-21","productIds":[2]}""", JsonConvert.SerializeObject(byProduct.ToBody()));

        var byVariants = new VariantPromotionRequest { VariantIds = new() { 2, 4 }, ProductIds = new() { 2 }, Percent = 20, StartDate = new DateTime(2026, 11, 12), EndDate = new DateTime(2026, 11, 21) };
        Assert.Equal("""{"percent":20.0,"rounding":"none","startDate":"2026-11-12","endDate":"2026-11-21","variantIds":[2,4],"previewHash":"h","allowAboveProductPromotion":true}""",
            JsonConvert.SerializeObject(byVariants.ToApplyBody("h", false, false, false, true)));
    }

    [Fact]
    public async Task Service_Routes()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new VariantPromotionPreview() };
        var service = new VariantPromotionService(fakeHttp);
        var request = new VariantPromotionRequest { ProductIds = new() { 2 }, Percent = 20 };

        await service.PreviewAsync(request);
        fakeHttp.ResultToReturn = new VariantPromotionApplyResult();
        await service.ApplyAsync(request, "h", false, false, false, false);
        await service.EndAsync(9);
        fakeHttp.ResultToReturn = new List<VariantPromotionItem>();
        await service.GetPromotionsAsync();

        Assert.Equal(new[] { "gestion/promotions/variantes/apercu", "gestion/promotions/variantes/appliquer", "gestion/promotions/variantes/terminer" },
            fakeHttp.PostedAuthData.Select(p => p.Url));
        Assert.Equal("""{"batchId":9}""", JsonConvert.SerializeObject(fakeHttp.PostedAuthData[2].Data));
        Assert.Equal("gestion/promotions/variantes?status=all", Assert.Single(fakeHttp.RequestedUrls));
    }

    [Fact]
    public void Item_Texts()
    {
        var item = new VariantPromotionItem { name = "T-shirt Horizon", variantLabel = "XL · Noir", percent = 12.5, price = 3412, basePrice = 3900, startDate = "2026-11-12", endDate = "2026-11-21", status = "upcoming", Currency = "CAD" };
        Assert.Equal("T-shirt Horizon · XL · Noir", item.Title);
        Assert.StartsWith("−12,5 % : ", item.PriceText);
        Assert.Equal("À venir", item.StatusText);
        Assert.True(item.CanEnd);
        var ending = new VariantPromotionItem { status = "active", endedAt = "2026-11-11T20:00:00+00:00" };
        Assert.Equal("Se termine ce soir", ending.StatusText);
        Assert.False(ending.CanEnd);
    }

    [Fact]
    public void ViewModel_ValidatesPercent_AndScopesToCheckedVariants()
    {
        var vm = new VariantPromotionViewModel(new VariantPromotionService(new FakeHttpService()), new FakeDialogService());
        vm.Init(2, "T-shirt Horizon");

        vm.PercentText = "4";
        Assert.Null(vm.BuildRequest(null, out var error));
        Assert.Contains("entre 5 et 90", error);

        vm.PercentText = "12,5";
        var all = vm.BuildRequest(null, out _);
        Assert.Equal(new List<int> { 2 }, all!.ProductIds);
        Assert.Null(all.VariantIds);
        Assert.Equal(12.5, all.Percent);

        var some = vm.BuildRequest(new[] { 2, 4 }, out _);
        Assert.Equal(new List<int> { 2, 4 }, some!.VariantIds);
        Assert.Null(some.ProductIds);

        // Une ligne bloquée (sans prix) n'est pas cochée par défaut.
        var preview = JsonConvert.DeserializeObject<VariantPromotionPreview>(PreviewJson)!;
        Assert.True(new VariantPromotionChoice(preview.rows[0]).IsSelected);
        Assert.False(new VariantPromotionChoice(preview.rows[3]).IsSelected);
    }
}
