using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Offline;
using Easymakemoney.ViewModels.Dashboard;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Revue du 12/11/2026 : sans réseau, l'Accueil montre sa dernière copie, datée, au lieu d'un écran vide.</summary>
public class HomeOfflineTests
{
    [Theory]
    [InlineData("Demo", 12, "demo:12")]
    [InlineData(" demo ", 3, "demo:3")]
    [InlineData(null, 12, null)]
    [InlineData("demo", null, null)]
    [InlineData("demo", 0, null)]
    public void Scope_PerSiteAndAccount(string? site, int? user, string? expected)
    {
        Assert.Equal(expected, DashboardPageViewModel.SnapshotScope(site, user));
    }

    [Fact]
    public async Task Online_SavesSnapshot()
    {
        var store = new MemoryStore();
        var tiles = new Tiles { Result = new() { new HomeTile { id = "advice", priority = 5, title = "Conseil" } } };
        var vm = new DashboardPageViewModel(tiles, null, store, () => "demo:12");

        await vm.LoadTilesAsync();

        Assert.False(vm.IsOffline);
        Assert.Equal("Conseil", store.Saved["demo:12"].Tiles.Single().title);
    }

    [Fact]
    public async Task Offline_ShowsLastSnapshotWithTime()
    {
        var store = new MemoryStore();
        store.Saved["demo:12"] = new HomeSnapshot
        {
            SavedAt = DateTime.Today.AddHours(14).AddMinutes(5),
            Tiles = { new HomeTile { id = "advice", title = "Conseil d'hier" } },
            Day = new DaySummary { sales = new DaySales { total = 1000, orders = 1 } },
            ReadinessSteps = { new ReadinessStep { key = "management.fiscal_year", status = "todo", blocking = true, appScreen = "exercice" } },
        };
        var tiles = new Tiles { Error = ApiException.NoConnection() };
        var vm = new DashboardPageViewModel(tiles, null, store, () => "demo:12");

        await vm.LoadTilesAsync();

        Assert.Equal("Hors connexion : données de 14 h 05", vm.OfflineText);
        Assert.Equal("Conseil d'hier", vm.Tiles.Single().title);
        Assert.True(vm.HasDay);
        Assert.True(vm.HasReadinessCard);
        Assert.Empty(store.Saved.Keys.Where(key => key != "demo:12"));
    }

    [Fact]
    public async Task Offline_OlderSnapshot_ShowsDate()
    {
        var store = new MemoryStore();
        store.Saved["demo:12"] = new HomeSnapshot { SavedAt = DateTime.Today.AddDays(-1).AddHours(9) };
        var vm = new DashboardPageViewModel(new Tiles { Error = ApiException.NoConnection() }, null, store, () => "demo:12");

        await vm.LoadTilesAsync();

        Assert.StartsWith("Hors connexion : données du ", vm.OfflineText);
        Assert.EndsWith("à 9 h 00", vm.OfflineText);
    }

    [Fact]
    public async Task Offline_NoSnapshot_EmptyAsBefore()
    {
        var vm = new DashboardPageViewModel(new Tiles { Error = ApiException.NoConnection() }, null, new MemoryStore(), () => "demo:12");

        await vm.LoadTilesAsync();

        Assert.False(vm.IsOffline);
        Assert.Empty(vm.Tiles);
    }

    [Fact]
    public async Task ServerError_DoesNotUseSnapshot()
    {
        // Seule l'absence de réseau montre la copie : une erreur du serveur ne doit pas faire croire à des données à jour.
        var store = new MemoryStore();
        store.Saved["demo:12"] = new HomeSnapshot { SavedAt = DateTime.Now, Tiles = { new HomeTile { id = "x" } } };
        var error = new ApiException(ApiError.FromHttpResponse("""{ "code": "INTERNAL" }""", HttpStatusCode.InternalServerError, null), HttpStatusCode.InternalServerError);
        var vm = new DashboardPageViewModel(new Tiles { Error = error }, null, store, () => "demo:12");

        await vm.LoadTilesAsync();

        Assert.False(vm.IsOffline);
        Assert.Empty(vm.Tiles);
    }

    [Fact]
    public async Task OtherAccount_DoesNotSeeSnapshot()
    {
        var store = new MemoryStore();
        store.Saved["demo:12"] = new HomeSnapshot { SavedAt = DateTime.Now, Tiles = { new HomeTile { id = "x" } } };
        var vm = new DashboardPageViewModel(new Tiles { Error = ApiException.NoConnection() }, null, store, () => "demo:99");

        await vm.LoadTilesAsync();

        Assert.False(vm.IsOffline);
        Assert.Empty(vm.Tiles);
    }

    [Fact]
    public void LineReceived_HasFrenchFallback()
    {
        Assert.StartsWith("Cette ligne a déjà été reçue", ApiError.GetFallbackMessage("LINE_RECEIVED", HttpStatusCode.Conflict, null));
    }

    private sealed class MemoryStore : IHomeSnapshotStore
    {
        public Dictionary<string, HomeSnapshot> Saved { get; } = new();
        public void Save(string scope, HomeSnapshot snapshot) => Saved[scope] = snapshot;
        public HomeSnapshot? Load(string scope) => Saved.TryGetValue(scope, out var snapshot) ? snapshot : null;
    }

    private sealed class Tiles : IHomeTilesService
    {
        public List<HomeTile> Result { get; set; } = new();
        public Exception? Error { get; set; }
        public Task<List<HomeTile>> GetTilesAsync(string locale) => Error != null ? Task.FromException<List<HomeTile>>(Error) : Task.FromResult(Result);
        public Task DismissAsync(string tileId) => Task.CompletedTask;
    }
}
