using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Statistiques;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Promotion de location par article (lot backend LOC3).</summary>
[Collection("SiteCurrency")]
public class RentalPromotionTests : IDisposable
{
    public RentalPromotionTests() => SiteCurrency.Reset();
    public void Dispose()
    {
        SiteCurrency.Reset();
        RentalStatisticsViewModel.PromotionsChanged = false;
    }

    // Réponse réelle prise sur demo par le backend (11/11/2026).
    private const string PreviewJson = """
    {"percent": 20, "startDate": "2026-11-12", "endDate": "2026-12-11", "days": 30, "weekdays": [6, 7], "adviceId": null,
     "items": [{"productId": 9, "name": "Planche à pagaie gonflable", "status": "ok", "conflicts": [], "example": {"durationType": "day", "rate": 9500, "discounted": 7600}}],
     "counts": {"ok": 1, "notRentable": 0, "overlap": 0}, "warnings": [],
     "previewHash": "dbd959e05005d15cf06d7bbb751c550fee8658d7b9dd0be65575ba2ac204e981"}
    """;

    [Theory]
    [InlineData(new int[0], "tous les jours")]
    [InlineData(new[] { 1, 2, 3, 4, 5, 6, 7 }, "tous les jours")]
    [InlineData(new[] { 6, 7 }, "le samedi et le dimanche")]
    [InlineData(new[] { 1, 2, 3, 4 }, "du lundi au jeudi")]
    [InlineData(new[] { 5, 1, 3 }, "le lundi, le mercredi et le vendredi")]
    [InlineData(new[] { 6 }, "le samedi")]
    public void Weekdays_Text(int[] days, string expected) => Assert.Equal(expected, RentalWeekdays.Text(days));

    [Fact]
    public void Preview_RealDemoResponse_CanApply()
    {
        var preview = JsonConvert.DeserializeObject<RentalPromotionPreview>(PreviewJson)!;

        Assert.True(preview.CanApply);
        Assert.False(preview.HasBlocking);
        Assert.Equal("−20 % du 12/11/2026 au 11/12/2026 (30 jours), le samedi et le dimanche.", preview.PeriodText);
        var row = Assert.Single(preview.Rows());
        Assert.True(row.IsOk);
        Assert.False(row.HasStatus);
        Assert.Contains("→", row.ExampleText);
        Assert.EndsWith("par jour", row.ExampleText);
    }

    [Fact]
    public void Preview_WithOverlapOrNotRentable_Blocks()
    {
        var preview = JsonConvert.DeserializeObject<RentalPromotionPreview>("""
        {"percent": 60, "items": [{"productId": 1, "name": "Kayak", "status": "PROMOTION_OVERLAP", "conflicts": [3], "example": null}, {"productId": 2, "name": "Tente", "status": "ok"}],
         "counts": {"ok": 1, "notRentable": 0, "overlap": 1}, "warnings": ["BIG_DISCOUNT"], "previewHash": "x"}
        """)!;

        Assert.False(preview.CanApply);
        Assert.Contains("déjà une promotion", preview.BlockingText);
        Assert.True(preview.HasBigDiscount);
        var rows = preview.Rows();
        Assert.Equal("Déjà en promotion sur ces jours", rows[0].StatusText);
        Assert.Equal("Pas de grille de tarifs", rows[0].ExampleText);
    }

    [Fact]
    public void Request_Body_ProductsOrAdvice_AndWeekdays()
    {
        var byProduct = new RentalPromotionRequest
        {
            ProductIds = new List<int> { 9 }, Percent = 20,
            StartDate = new DateTime(2026, 11, 12), EndDate = new DateTime(2026, 12, 11), Weekdays = new List<int> { 7, 6 },
        };
        Assert.Equal("""{"percent":20,"startDate":"2026-11-12","endDate":"2026-12-11","productIds":[9],"weekdays":[6,7],"previewHash":"h"}""",
            JsonConvert.SerializeObject(byProduct.ToBody("h")));

        var byAdvice = new RentalPromotionRequest
        {
            AdviceId = 5, ProductIds = new List<int> { 9 }, Percent = 20,
            StartDate = new DateTime(2026, 11, 12), EndDate = new DateTime(2026, 12, 11), Weekdays = Enumerable.Range(1, 7).ToList(),
        };
        // Le conseil prime sur les articles ; les 7 jours = tous les jours, donc pas de weekdays.
        Assert.Equal("""{"percent":20,"startDate":"2026-11-12","endDate":"2026-12-11","adviceId":5}""", JsonConvert.SerializeObject(byAdvice.ToBody()));
    }

    [Fact]
    public async Task Service_Routes()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new RentalPromotionPreview() };
        var service = new RentalPromotionService(fakeHttp);
        var request = new RentalPromotionRequest { ProductIds = new List<int> { 9 }, Percent = 20 };

        await service.PreviewAsync(request);
        fakeHttp.ResultToReturn = new RentalPromotionApplyResult();
        await service.ApplyAsync(request, "h");
        await service.EndAsync(4);
        fakeHttp.ResultToReturn = new List<RentalPromotionItem>();
        await service.GetPromotionsAsync();

        Assert.Equal(new[] { "gestion/locations/promotions/apercu", "gestion/locations/promotions/appliquer", "gestion/locations/promotions/terminer" },
            fakeHttp.PostedAuthData.Select(p => p.Url));
        Assert.Equal("""{"batchId":4}""", JsonConvert.SerializeObject(fakeHttp.PostedAuthData[2].Data));
        Assert.Equal("gestion/locations/promotions", Assert.Single(fakeHttp.RequestedUrls));
    }

    [Fact]
    public void Item_Texts_AndEndability()
    {
        var item = new RentalPromotionItem { batchId = 1, name = "Ponton", percent = 20, startDate = "2026-11-21", endDate = "2026-12-28", weekdays = new() { 6, 7 }, status = "upcoming" };
        Assert.Equal("Ponton · −20 %", item.Title);
        Assert.Equal("Du 21/11/2026 au 28/12/2026, le samedi et le dimanche", item.DatesText);
        Assert.Equal("À venir", item.StatusText);
        Assert.True(item.CanEnd);
        Assert.False(new RentalPromotionItem { status = "cancelled" }.CanEnd);

        var ending = new RentalPromotionItem { status = "active", endedAt = "2026-11-11T18:00:00+00:00" };
        Assert.Equal("Se termine ce soir", ending.StatusText);
        Assert.False(ending.CanEnd);
        Assert.True(ending.IsVisible);
    }

    [Fact]
    public void ViewModel_ValidatesPercentAndKeepsOneWeekday()
    {
        var vm = new RentalPromotionViewModel(new RentalPromotionService(new FakeHttpService()), new FakeDialogService());
        vm.Init(0, 9, "Planche", 20, "6,7");

        Assert.Equal(new[] { "6", "7" }, vm.Weekdays.Where(d => d.IsSelected).Select(d => d.Code));
        vm.ToggleWeekdayCommand.Execute("6");
        vm.ToggleWeekdayCommand.Execute("7"); // le dernier jour coché reste coché
        Assert.Equal(new[] { "7" }, vm.Weekdays.Where(d => d.IsSelected).Select(d => d.Code));

        vm.PercentText = "12,5";
        Assert.Null(vm.BuildRequest(out var error));
        Assert.Contains("entier", error);

        vm.PercentText = "25";
        var request = vm.BuildRequest(out _);
        Assert.NotNull(request);
        Assert.Equal(new List<int> { 9 }, request!.ProductIds);
        Assert.Equal(new List<int> { 7 }, request.Weekdays);
    }
}
