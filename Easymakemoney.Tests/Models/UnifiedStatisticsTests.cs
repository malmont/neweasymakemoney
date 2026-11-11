using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Statistiques unifiées (lot backend S1).</summary>
[Collection("SiteCurrency")]
public class UnifiedStatisticsTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public UnifiedStatisticsTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private const string Json = """
    { "currency": "CAD", "source": "all",
      "range": { "type": "month", "from": "2026-11-01", "to": "2026-11-11", "days": 11, "granularity": "day" },
      "previous": { "from": "2026-10-01", "to": "2026-10-11" }, "lastYear": { "from": "2025-11-01", "to": "2025-11-11" },
      "comparison": { "currentLabel": "1-11 nov.", "previousLabel": "1-11 oct.", "lastYearLabel": "1-11 nov. 2025" },
      "indicators": {
        "revenue": { "value": 117070, "unit": "money", "previous": 100000, "lastYear": 130000, "changeVsPrevious": 17.1, "changeVsLastYear": -9.9, "changeUnit": "percent", "spark": [ 0, 1200, null, 5000 ] },
        "grossMargin": { "value": 51225, "unit": "money", "previous": null, "lastYear": null, "changeVsPrevious": null, "changeVsLastYear": null, "changeUnit": "percent", "spark": [] },
        "grossMarginRate": { "value": 43.9, "unit": "percent", "changeVsPrevious": -1.5, "changeVsLastYear": 0, "changeUnit": "points" },
        "orders": { "value": 12, "unit": "count", "changeVsPrevious": 20, "changeUnit": "percent" },
        "averageBasket": { "value": 9756, "unit": "money" },
        "returnRate": { "value": 4.2, "unit": "percent", "changeVsPrevious": 1.1, "changeUnit": "points", "returns": { "amount": 5000, "orders": 2 } },
        "stockValue": { "value": 2588125, "unit": "money", "costValue": 1200000, "previous": null, "lastYear": null, "changeUnit": "percent" } },
      "otherRevenue": { "rentals": { "value": 31900, "unit": "money", "previous": 10000, "lastYear": 0, "changeVsPrevious": 219, "changeVsLastYear": null, "changeUnit": "percent" },
                        "subscriptions": { "value": 0, "unit": "money", "previous": 0, "lastYear": 0 } },
      "chart": { "granularity": "day", "points": [
        { "key": "2026-11-01", "from": "2026-11-01", "to": "2026-11-01", "revenue": 0, "grossMargin": 0, "orders": 0, "lastYear": { "revenue": 1000, "grossMargin": 400, "orders": 1 } },
        { "key": "2026-11-02", "from": "2026-11-02", "to": "2026-11-02", "revenue": 5000, "grossMargin": 2000, "orders": 2, "lastYear": null } ] },
      "caveats": [ { "code": "UNKNOWN_COST", "text": "4,50 $ de ventes sans coût connu.", "amount": 450 }, { "code": "PARTIAL_DAY", "text": "Aujourd'hui n'est pas terminé." } ] }
    """;

    private static UnifiedStatistics Read() => JsonConvert.DeserializeObject<UnifiedStatistics>(Json)!;

    [Fact]
    public void Cards_InReadingOrder_WithChangesAndTones()
    {
        var cards = Read().Cards();
        Assert.Equal(4, StatsCardRow.Pair(cards).Count);
        Assert.Null(StatsCardRow.Pair(cards)[3].Right);

        Assert.Equal(new[] { "revenue", "grossMargin", "grossMarginRate", "orders", "averageBasket", "returnRate", "stockValue" }, cards.Select(c => c.Key));

        var revenue = cards[0];
        Assert.Contains("1", revenue.ValueText);
        Assert.Equal("+17,1 % vs précédent", revenue.PreviousText);
        Assert.Equal("−9,9 % vs an dernier", revenue.LastYearText);
        Assert.Equal(1, revenue.PreviousTone);
        Assert.Equal(-1, revenue.LastYearTone);
        Assert.Equal(new double[] { 0, 1200, 0, 5000 }, revenue.Spark);

        var rate = cards[2];
        Assert.Equal("43,9 %", rate.ValueText);
        Assert.Equal("−1,5 pt vs précédent", rate.PreviousText);
        Assert.Equal("0 pt vs an dernier", rate.LastYearText);
        Assert.Equal(0, rate.LastYearTone);

        var returns = cards[5];
        Assert.Equal(-1, returns.PreviousTone); // plus de retours : mauvais signe
        Assert.Contains("sur 2 vente(s)", returns.Detail);

        Assert.Equal("12", cards[3].ValueText);
        Assert.Empty(cards[1].PreviousText);
        Assert.False(cards[1].HasSpark);
        Assert.Contains("au coût", cards[6].Detail);
        Assert.Equal(0, cards[6].PreviousTone);
    }

    [Fact]
    public void ComparisonText_ListsBothComparisons_OrOnlyLastYear()
    {
        Assert.Equal("1-11 nov. · comparé à 1-11 oct. et à 1-11 nov. 2025", Read().ComparisonText);

        var year = Read();
        year.comparison!.previousLabel = null;
        Assert.Equal("1-11 nov. · comparé à 1-11 nov. 2025", year.ComparisonText);
    }

    [Fact]
    public void NoPrevious_HidesPreviousChange()
    {
        var data = Read();
        data.previous = null;

        Assert.Empty(data.Cards()[0].PreviousText);
        Assert.Equal("−9,9 % vs an dernier", data.Cards()[0].LastYearText);
    }

    [Fact]
    public void OtherRevenue_KeepsOnlyRowsWithValues()
    {
        var row = Assert.Single(Read().OtherRevenueRows());
        Assert.Equal("Locations", row.Title);
        Assert.Equal("+219 % vs précédent", row.ChangeText);
    }

    [Fact]
    public void ChartPoints_FollowMetric_WithLastYear()
    {
        var data = Read();

        var revenue = data.ChartPoints("revenue");
        Assert.Equal(2, revenue.Count);
        Assert.Equal(0, revenue[0].Value);
        Assert.Equal(1000, revenue[0].LastYear);
        Assert.Null(revenue[1].LastYear);
        Assert.Equal("1 nov.", revenue[0].ShortLabel);

        var orders = data.ChartPoints("orders");
        Assert.Equal(2, orders[1].Value);
        Assert.Equal(1, orders[0].LastYear);
        Assert.Equal(2000, data.ChartPoints("grossMargin")[1].Value);
        Assert.Contains("• Aujourd'hui n'est pas terminé.", data.CaveatsText);
    }

    [Fact]
    public void PointLabels_FollowGranularity()
    {
        var point = new StatsPoint { key = "2026-W45", from = "2026-11-02", to = "2026-11-08" };
        Assert.Equal("sem. du 2 nov.", StatsFormat.PointLabel(point, "week"));
        Assert.StartsWith("nov", StatsFormat.ShortLabel(new StatsPoint { key = "2026-11", from = "2026-11-01" }, "month"));
    }

    [Fact]
    public async Task Service_BuildsQueryForEachKindOfRange()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new UnifiedStatistics() };
        var service = new UnifiedStatisticsService(fakeHttp);

        await service.GetAsync(new StatsQuery("7d", Source: "pos"));
        await service.GetAsync(new StatsQuery(null, PeriodId: 4));
        await service.GetAsync(new StatsQuery(null, From: new DateTime(2026, 9, 1), To: new DateTime(2026, 11, 10), Source: "ecommerce"));
        await service.GetAsync(new StatsQuery(null));

        Assert.Equal(new[]
        {
            "gestion/statistiques?range=7d&source=pos&locale=fr",
            "gestion/statistiques?periodId=4&source=all&locale=fr",
            "gestion/statistiques?from=2026-09-01&to=2026-11-10&source=ecommerce&locale=fr",
            "gestion/statistiques?range=month&source=all&locale=fr",
        }, fakeHttp.RequestedUrls);
    }
}

public class StatistiquesPeriodTests
{
    [Fact]
    public void CurrentPeriod_ContainsToday()
    {
        var autumn = new ListCollection { id = 1, nomCollection = "Automne 2026", photoCollection = "", startDateCollection = "2026-09-01", endDateCollection = "2026-11-30" };
        var winter = new ListCollection { id = 2, nomCollection = "Hiver 2026-2027", photoCollection = "", startDateCollection = "2026-12-01T00:00:00+00:00", endDateCollection = "2027-02-28T00:00:00+00:00" };

        Assert.True(Easymakemoney.ViewModels.Statistiques.StatistiquesViewModel.Contains(autumn, new DateTime(2026, 11, 10)));
        Assert.False(Easymakemoney.ViewModels.Statistiques.StatistiquesViewModel.Contains(winter, new DateTime(2026, 11, 10)));
        Assert.True(Easymakemoney.ViewModels.Statistiques.StatistiquesViewModel.Contains(winter, new DateTime(2027, 2, 28)));
    }
}
