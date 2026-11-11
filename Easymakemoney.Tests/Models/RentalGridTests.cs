using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Statistiques;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Changement de grille de location (lot backend LOC4).</summary>
[Collection("SiteCurrency")]
public class RentalGridTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public RentalGridTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    [Fact]
    public void Grid_Texts()
    {
        var grid = JsonConvert.DeserializeObject<RentalGrid>("""
        { "id": 3, "name": "Tarif nautique", "rates": { "hour": 2500, "halfDay": null, "day": 9500, "week": null, "month": null },
          "products": [ { "productId": 1, "name": "Kayak" }, { "productId": 9, "name": "Planche" } ] }
        """)!;
        grid.Currency = "CAD";

        Assert.Contains("l'heure", grid.RatesText);
        Assert.Contains("la journée", grid.RatesText);
        Assert.DoesNotContain("semaine", grid.RatesText);
        Assert.Equal("2 articles : Kayak, Planche", grid.ProductsText);
        Assert.True(grid.IsShared);
    }

    [Fact]
    public void Preview_SharedAndBigChange_AndChangeLines()
    {
        var preview = JsonConvert.DeserializeObject<RentalGridPreview>("""
        { "pack": { "id": 3, "name": "Tarif nautique" }, "before": { "day": 9500 }, "after": { "day": 10500 },
          "changes": [ { "type": "day", "before": 9500, "after": 10500, "percent": 10.5 } ],
          "products": [ { "productId": 1, "name": "Kayak" }, { "productId": 9, "name": "Planche" } ], "shared": true,
          "warnings": [ "SHARED_GRID" ], "previewHash": "h" }
        """)!;

        Assert.True(preview.IsShared);
        Assert.False(preview.HasBigChange);
        Assert.Contains("2 articles", preview.SharedText);
        Assert.Contains("promotion de location", preview.SharedText);
        var line = Assert.Single(preview.ChangeLines());
        Assert.StartsWith("La journée : ", line);
        Assert.EndsWith("(+10,5 %)", line);
    }

    [Fact]
    public void Request_Body()
    {
        var request = new RentalGridRequest { RentalPackId = 3, Percent = 10, RoundToDollar = true };
        Assert.Equal("""{"rentalPackId":3,"percent":10,"rounding":"dollar"}""", JsonConvert.SerializeObject(request.ToBody()));
        Assert.Equal("""{"rentalPackId":3,"percent":10,"rounding":"dollar","previewHash":"h","allowShared":true}""",
            JsonConvert.SerializeObject(request.ToBody("h", allowShared: true)));
    }

    [Fact]
    public void Modification_UndoWindow()
    {
        var now = new DateTimeOffset(2026, 11, 11, 18, 0, 0, TimeSpan.Zero);
        var open = new RentalGridModification { undoUntil = "2026-11-12T17:00:00+00:00" };
        var expired = new RentalGridModification { undoUntil = "2026-11-11T17:00:00+00:00" };
        var undone = new RentalGridModification { undoUntil = "2026-11-12T17:00:00+00:00", undoneAt = "2026-11-11T17:30:00+00:00" };

        Assert.True(open.CanUndoAt(now));
        Assert.False(expired.CanUndoAt(now));
        Assert.False(undone.CanUndoAt(now));
        Assert.Equal("Annulé", undone.StatusText);
    }

    [Fact]
    public async Task Service_Routes()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new List<RentalGrid>() };
        var service = new RentalGridService(fakeHttp);
        var request = new RentalGridRequest { RentalPackId = 3, Percent = -10 };

        await service.GetGridsAsync();
        fakeHttp.ResultToReturn = new RentalGridPreview();
        await service.PreviewAsync(request);
        await service.ApplyAsync(request, "h", allowShared: false);
        await service.UndoAsync(7);
        fakeHttp.ResultToReturn = new List<RentalGridModification>();
        await service.GetModificationsAsync(5);

        Assert.Equal(new[] { "gestion/locations/grilles", "gestion/locations/grilles/modifications?limit=5" }, fakeHttp.RequestedUrls);
        Assert.Equal(new[] { "gestion/locations/grilles/apercu", "gestion/locations/grilles/appliquer", "gestion/locations/grilles/annuler" },
            fakeHttp.PostedAuthData.Select(p => p.Url));
        Assert.DoesNotContain("allowShared", JsonConvert.SerializeObject(fakeHttp.PostedAuthData[1].Data));
        Assert.Equal("""{"changeId":7}""", JsonConvert.SerializeObject(fakeHttp.PostedAuthData[2].Data));
    }

    [Theory]
    [InlineData("-10", -10.0)]
    [InlineData("−15", -15.0)]
    [InlineData("+20", 20.0)]
    [InlineData(" 30 ", 30.0)]
    public void SignedPercent_Parses(string text, double expected) => Assert.Equal(expected, RentalGridViewModel.ParseSignedPercent(text));

    [Fact]
    public void ViewModel_ValidatesPercent()
    {
        var vm = new RentalGridViewModel(new RentalGridService(new FakeHttpService()), new FakeDialogService());

        Assert.Null(vm.BuildRequest(out var error));
        Assert.Contains("grille", error);

        vm.SelectGridCommand.Execute(new RentalGrid { id = 3, name = "Tarif nautique" });
        foreach (var bad in new[] { "0", "-60", "250", "12,5", "abc" })
        {
            vm.PercentText = bad;
            Assert.Null(vm.BuildRequest(out _));
        }

        vm.PercentText = "-10";
        vm.RoundToDollar = false;
        var request = vm.BuildRequest(out _);
        Assert.Equal(3, request!.RentalPackId);
        Assert.Equal(-10, request.Percent);
        Assert.False(request.RoundToDollar);

        // « Baisse » choisie : un pourcentage saisi sans signe devient négatif ; au-delà de 50 %, refusé.
        vm.SetDirectionCommand.Execute("down");
        vm.PercentText = "15";
        Assert.Equal(-15, vm.BuildRequest(out _)!.Percent);
        vm.PercentText = "60";
        Assert.Null(vm.BuildRequest(out var tooMuch));
        Assert.Contains("entre 1 et 50", tooMuch);
        vm.SetDirectionCommand.Execute("up");
        vm.PercentText = "60";
        Assert.Equal(60, vm.BuildRequest(out _)!.Percent);
    }
}
