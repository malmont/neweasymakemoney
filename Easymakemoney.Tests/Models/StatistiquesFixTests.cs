using Easymakemoney.Models;
using Easymakemoney.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class StatistiquesFixTests
{
    [Fact]
    public void StockValue_IsSentInDollars_AndDisplayedFromCents()
    {
        var data = JsonConvert.DeserializeObject<StockValueData>("""{ "month": "November 2026", "stock_value": 43007.6 }""")!;

        Assert.Equal(4300760, data.StockValueCents);
        Assert.Equal("novembre 2026", data.MonthText);

        var fromServer = JsonConvert.DeserializeObject<StockValueData>("""{ "month": "x", "stock_value": 1, "stock_value_cents": 250 }""")!;
        Assert.Equal(250, fromServer.StockValueCents);
    }

    [Fact]
    public void Chart_AcceptsNullAndQuantities()
    {
        Assert.Empty(Assert.IsType<Microcharts.PointChart>(ChartGenerator.GenerateChart(null)).Entries);
    }
}

public class StockMovementLabelTests
{
    [Fact]
    public void Movements_UseFrenchLabelsFromCode()
    {
        var items = JsonConvert.DeserializeObject<List<StockEvolutionData>>("""
        [ { "type": "Entrant", "quantity": 3, "code": "in" }, { "type": "Return", "quantity": 0, "code": "return" },
          { "type": "Réception fournisseur", "quantity": 7, "code": "reception" }, { "type": "Return", "quantity": 1 } ]
        """)!;

        Assert.Equal(new[] { "Entrées", "Retours", "Réceptions fournisseur", "Retours" }, items.Select(i => i.Data1));
    }
}

public class StatistiquesBackendFixTests
{
    [Fact]
    public void OrderCounts_PreferSalesOnlyKeys_WhenPresent()
    {
        var stats = JsonConvert.DeserializeObject<OrderStatistics>("""
        { "currentMonthCount": 12, "currentYearCount": 120, "lastYearCount": 90,
          "currentMonthSalesCount": 9, "currentYearSalesCount": 100, "lastYearSalesCount": 80,
          "monthlySalesCountForCurrentYear": [ { "month": "1", "orderCount": 7 } ] }
        """)!;

        Assert.Equal(9, stats.CurrentMonthCount);
        Assert.Equal(100, stats.CurrentYearCount);
        Assert.Equal(80, stats.LastYearCount);
        Assert.Single(stats.MonthlyCountForCurrentYear);

        var old = JsonConvert.DeserializeObject<OrderStatistics>("""{ "currentMonthCount": 12 }""")!;
        Assert.Equal(12, old.CurrentMonthCount);
    }

    [Fact]
    public void AverageBasket_ReadsLastYear_AndCarrierUsesBilledShipping()
    {
        Assert.Equal(7287, JsonConvert.DeserializeObject<AverageOrderValueStatistics>("""{ "lastYearAverage": 7287 }""")!.LastYearData);
        Assert.Equal(0, JsonConvert.DeserializeObject<AverageOrderValueStatistics>("{}")!.LastYearData);

        var carrier = JsonConvert.DeserializeObject<MonthlyCarrierData>("""{ "month": "1", "carrier_count": 12000, "shippingBilled": 9000 }""")!;
        Assert.Equal(9000, carrier.Data2);
    }

    [Fact]
    public async Task CashTransactions_UseListModeAndNeverFailOnEmpty()
    {
        var fakeHttp = new Easymakemoney.Tests.Fakes.FakeHttpService();
        var service = new Easymakemoney.Services.TransactionCaisseService(fakeHttp);

        var transactions = await service.GetTransactions();

        Assert.Empty(transactions);
        Assert.Equal("caisse/transactions?asList=1", Assert.Single(fakeHttp.RequestedUrls));
    }
}
