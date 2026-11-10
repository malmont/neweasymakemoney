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
