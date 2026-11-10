using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Utilities;
using Xunit;

namespace Easymakemoney.Tests.Models;

[Collection("SiteCurrency")]
public class ListCommandTests : IDisposable
{
    private readonly CultureInfo _originalCulture;

    public ListCommandTests()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        SiteCurrency.Reset();
        CultureInfo.CurrentCulture = _originalCulture;
    }

    private static ListCommand MakeCommand(long? budget = null, string? currency = null) => new()
    {
        name = "Commande Test",
        date = "2026-10-15T00:00:00Z",
        photo = "photo.png",
        budget = budget,
        currency = currency
    };

    [Fact]
    public void BudgetText_WhenBudgetNull_ReturnsEmpty()
    {
        var cmd = MakeCommand(budget: null);
        Assert.Equal(string.Empty, cmd.BudgetText);
    }

    [Fact]
    public void BudgetText_WhenCurrencySpecified_FormatsInCommandCurrencyNotSiteCurrency()
    {
        SiteCurrency.SetCurrency("EUR");
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        var cmd = MakeCommand(budget: 150000, currency: "USD");
        var text = cmd.BudgetText;

        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.True(text.Contains("$") || text.Contains("US$"));
        Assert.DoesNotContain("€", text);
        Assert.Contains("1", text);
        Assert.Contains("500", text);
    }

    [Fact]
    public void BudgetText_WhenCurrencyNull_FallsBackToSiteCurrency()
    {
        SiteCurrency.SetCurrency("EUR");
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        var cmd = MakeCommand(budget: 150000, currency: null);
        var text = cmd.BudgetText;

        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("€", text);
        Assert.DoesNotContain("$", text);
        Assert.Contains("1", text);
        Assert.Contains("500", text);
    }

    [Fact]
    public void BudgetText_CultureFormatting_UsesCultureSeparators()
    {
        SiteCurrency.SetCurrency("EUR");
        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        var cmd = MakeCommand(budget: 150000, currency: "USD");
        var text = cmd.BudgetText;

        Assert.Contains("$", text);
        Assert.Contains("1,500.00", text);
        Assert.DoesNotContain("€", text);
    }
}
