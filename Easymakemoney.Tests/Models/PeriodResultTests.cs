using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

[Collection("SiteCurrency")]
public class PeriodResultTests : IDisposable
{
    private readonly CultureInfo _originalCulture;

    public PeriodResultTests()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        SiteCurrency.Reset();
        CultureInfo.CurrentCulture = _originalCulture;
    }

    [Fact]
    public void PeriodResult_DeserializesFullContractP2Json()
    {
        SiteCurrency.SetCurrency("EUR");
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        var json = """
        {
            "frozen": true,
            "sales": { "revenue": 129998, "unknownCostRevenue": 15000 },
            "otherRevenue": { "total": 20000 },
            "cogs": { "amount": 70818, "unknownCostItems": 3, "estimatedItems": 2, "estimatedAmount": 25000 },
            "expenses": { "shipping": 5000, "notes": 4000, "other": [ { "amount": 1000 } ] },
            "result": { "gross": 59180, "net": -40620, "netRate": -31.2 },
            "investment": { "total": 99800 }
        }
        """;

        var pr = JsonConvert.DeserializeObject<PeriodResult>(json);

        Assert.NotNull(pr);
        Assert.True(pr.frozen);
        Assert.Equal(129998L, pr.sales.revenue);
        Assert.Equal(15000L, pr.sales.unknownCostRevenue);
        Assert.Equal(20000L, pr.otherRevenue.total);
        Assert.Equal(70818L, pr.cogs.amount);
        Assert.Equal(3, pr.cogs.unknownCostItems);
        Assert.Equal(2, pr.cogs.estimatedItems);
        Assert.Equal(25000L, pr.cogs.estimatedAmount);
        Assert.Equal(5000L, pr.expenses.shipping);
        Assert.Equal(4000L, pr.expenses.notes);
        Assert.NotNull(pr.expenses.other);
        Assert.Single(pr.expenses.other);
        Assert.Equal(1000L, pr.expenses.other[0].amount);
        Assert.Equal(59180L, pr.result.gross);
        Assert.Equal(-40620L, pr.result.net);
        Assert.Equal(-31.2, pr.result.netRate);
        Assert.Equal(99800L, pr.investment.total);

        Assert.True(pr.IsLoss);
        Assert.True(pr.HasUnknownCost);
        Assert.True(pr.HasUnknownRevenue);
        Assert.True(pr.HasEstimatedCogs);
        Assert.True(pr.HasOtherExpenses);
        Assert.True(pr.HasOtherRevenue);
        Assert.True(pr.HasNetRate);
    }

    [Fact]
    public void NetRateText_WithEnUsCulture_FormatsWithDot()
    {
        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        var pr = new PeriodResult
        {
            result = new PeriodTotals { netRate = 15.2 }
        };

        Assert.True(pr.HasNetRate);
        Assert.Contains("15.2", pr.NetRateText);
        Assert.Contains("% des ventes", pr.NetRateText);
    }

    [Fact]
    public void NetRateText_WithFrCaCulture_FormatsWithComma()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");

        var pr = new PeriodResult
        {
            result = new PeriodTotals { netRate = 15.2 }
        };

        Assert.True(pr.HasNetRate);
        Assert.Contains("15,2", pr.NetRateText);
        Assert.Contains("% des ventes", pr.NetRateText);
    }

    [Fact]
    public void NetRateText_WhenNull_ReturnsEmptyString()
    {
        var pr = new PeriodResult
        {
            result = new PeriodTotals { netRate = null }
        };

        Assert.False(pr.HasNetRate);
        Assert.Equal(string.Empty, pr.NetRateText);
    }

    [Theory]
    [InlineData(-40620, true)]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(5000, false)]
    public void IsLoss_IdentifiesNegativeNetResult(long net, bool expectedIsLoss)
    {
        var pr = new PeriodResult
        {
            result = new PeriodTotals { net = net }
        };

        Assert.Equal(expectedIsLoss, pr.IsLoss);
    }

    [Fact]
    public void UnknownCostTitle_HandlesSingularPluralAndZero()
    {
        var prZero = new PeriodResult { cogs = new PeriodCogs { unknownCostItems = 0 } };
        Assert.False(prZero.HasUnknownCost);

        var prSingle = new PeriodResult { cogs = new PeriodCogs { unknownCostItems = 1 } };
        Assert.True(prSingle.HasUnknownCost);
        Assert.Equal("1 article sans coût", prSingle.UnknownCostTitle);

        var prMultiple = new PeriodResult { cogs = new PeriodCogs { unknownCostItems = 4 } };
        Assert.True(prMultiple.HasUnknownCost);
        Assert.Equal("4 articles sans coût", prMultiple.UnknownCostTitle);
    }

    [Fact]
    public void EstimatedCogs_WhenPresent_ReturnsEstimatedText()
    {
        SiteCurrency.SetCurrency("EUR");
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        var prWithEst = new PeriodResult
        {
            cogs = new PeriodCogs { estimatedItems = 3, estimatedAmount = 25000 }
        };

        Assert.True(prWithEst.HasEstimatedCogs);
        Assert.Contains("dont", prWithEst.EstimatedCogsText);
        Assert.Contains("estimés", prWithEst.EstimatedCogsText);
        Assert.Contains("250", prWithEst.EstimatedCogsText);
        Assert.Contains("€", prWithEst.EstimatedCogsText);

        var prWithoutEst = new PeriodResult
        {
            cogs = new PeriodCogs { estimatedItems = 0, estimatedAmount = 0 }
        };

        Assert.False(prWithoutEst.HasEstimatedCogs);
    }

    [Fact]
    public void OtherExpenses_HandlesNullOtherListWithoutException()
    {
        var pr = new PeriodResult
        {
            expenses = new PeriodExpenses { other = null }
        };

        Assert.False(pr.HasOtherExpenses);
        Assert.Contains("0", pr.OtherExpensesText);
    }
}

