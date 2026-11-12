using System.Globalization;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Dashboard;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Lot JOUR : « Ma journée » (GET /api/gestion/journee), contrat validé le 12/11/2026.</summary>
[Collection("SiteCurrency")]
public class DaySummaryTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public DaySummaryTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    // Jeudi 12/11/2026 ; forme du contrat JOUR.
    private const string Json = """
    { "date": "2026-11-12", "timezone": "America/Toronto", "currency": "CAD", "generatedAt": "2026-11-12T15:02:00-05:00",
      "sales": { "total": 52770, "orders": 3,
        "byChannel": [ { "channel": "ecommerce", "total": 30000, "orders": 1 }, { "channel": "pos", "total": 22770, "orders": 2 }, { "channel": "mobile_app", "total": 0, "orders": 0 } ],
        "vsSameWeekdayAverage": { "percent": 12.5, "average": 46907, "weeks": 4, "asOf": "15:02" } },
      "receipts": { "total": 60400, "byMethod": [ { "method": "cash", "total": 20400 }, { "method": "card", "total": 40000 } ] },
      "toShip": { "count": 2, "oldestAt": "2026-11-10T09:00:00-05:00" },
      "support": { "open": 3, "overdue": 1 },
      "cash": { "balance": 1000000, "balanceDate": "2026-11-11", "lowestIn14Days": { "amount": 850000, "date": "2026-11-20" }, "state": "warning" },
      "stock": { "lowCount": 4 },
      "screens": { "sales": "statistiques", "toShip": "commandes.a_expedier", "support": "assistance", "cash": "tresorerie", "stock": "stock.low" } }
    """;

    private static DaySummary Parse() => JsonConvert.DeserializeObject<DaySummary>(Json)!;

    [Fact]
    public void Texts_FromServerValues()
    {
        var day = Parse();

        Assert.EndsWith("3 commandes", day.SalesText);
        Assert.Equal("+12,5 % par rapport à un jeudi habituel à 15 h 02", day.SalesTrendText);
        Assert.True(day.IsSalesUp);
        Assert.Equal("2 commandes à expédier", day.ToShipText);
        Assert.Equal("3 demandes en attente, dont 1 en retard", day.SupportText);
        Assert.True(day.IsSupportLate);
        Assert.StartsWith("Solde :", day.CashText);
        Assert.StartsWith("Point bas sur 14 jours :", day.CashLowText);
        Assert.True(day.IsCashWarning);
        Assert.Equal("4 articles en stock bas", day.StockText);
        Assert.Equal("tresorerie", day.Screen("cash"));
    }

    [Fact]
    public void Texts_EmptyDay()
    {
        var day = new DaySummary
        {
            sales = new DaySales { total = 0, orders = 0, vsSameWeekdayAverage = new DayTrend { percent = null } },
            toShip = new DayToShip { count = 0 },
            stock = new DayStock { lowCount = 0 },
        };

        Assert.EndsWith("0 commande", day.SalesText);
        Assert.Null(day.SalesTrendText);
        Assert.False(day.HasSalesTrend);
        Assert.Equal("Rien à expédier", day.ToShipText);
        Assert.False(day.HasSupport);
        Assert.False(day.HasCash);
        Assert.Equal("Aucun article en stock bas", day.StockText);
    }

    [Fact]
    public void Trend_Down_UsesMinusSign()
    {
        var day = new DaySummary { date = "2026-11-12", sales = new DaySales { vsSameWeekdayAverage = new DayTrend { percent = -8 } } };

        Assert.Equal("−8 % par rapport à un jeudi habituel", day.SalesTrendText);
        Assert.True(day.IsSalesDown);
    }

    [Theory]
    [InlineData("15:02", "15 h 02")]
    [InlineData("09:00", "9 h 00")]
    [InlineData("9:5", "9 h 05")]
    [InlineData("midi", "midi")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void HourText_FrenchStyle(string? asOf, string? expected)
    {
        Assert.Equal(expected, DaySummary.HourText(asOf));
    }

    [Theory]
    [InlineData("statistiques", "StatistiquesPage?Range=today")]
    [InlineData("tresorerie", "TreasuryPage")]
    [InlineData("assistance", "SupportTicketListPage")]
    [InlineData("commandes.a_expedier", null)]
    [InlineData("stock.low", null)]
    [InlineData(null, null)]
    public void Routes(string? screen, string? route)
    {
        Assert.Equal(route, DashboardPageViewModel.DayRoute(screen));
    }

    [Fact]
    public async Task Service_Route()
    {
        var http = new FakeHttpService { ResultToReturn = new DaySummary() };

        await new ManagementModuleService(http).GetDayAsync();

        Assert.Equal("gestion/journee?locale=fr", http.RequestedUrls.Single());
    }

    [Fact]
    public async Task Dashboard_ShowsDayAndOpensScreens()
    {
        var service = new ManagementModuleTests.FakeManagementModuleService { Day = Parse(), Readiness = new SiteReadiness() };
        var opened = new List<string>();
        var vm = new DashboardPageViewModel(new NoTiles(), service) { NavigateAsync = route => { opened.Add(route); return Task.CompletedTask; } };

        await vm.LoadTilesAsync();
        await vm.OpenDayCommand.ExecuteAsync("sales");
        await vm.OpenDayCommand.ExecuteAsync("toShip");
        await vm.OpenDayCommand.ExecuteAsync("cash");

        Assert.True(vm.HasDay);
        Assert.Equal(new[] { "StatistiquesPage?Range=today", "TreasuryPage" }, opened);
    }

    [Fact]
    public async Task Dashboard_RouteMissing_HidesCardWithoutError()
    {
        var service = new ManagementModuleTests.FakeManagementModuleService
        {
            DayError = new ApiException(ApiError.FromHttpResponse("""{ "code": "NOT_FOUND" }""", HttpStatusCode.NotFound, null), HttpStatusCode.NotFound),
            Readiness = new SiteReadiness(),
        };
        var vm = new DashboardPageViewModel(new NoTiles(), service);

        await vm.LoadTilesAsync();

        Assert.False(vm.HasDay);
    }

    private sealed class NoTiles : IHomeTilesService
    {
        public Task<List<HomeTile>> GetTilesAsync(string locale) => Task.FromResult(new List<HomeTile>());
        public Task DismissAsync(string tileId) => Task.CompletedTask;
    }
}
