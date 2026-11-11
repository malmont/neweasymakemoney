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
