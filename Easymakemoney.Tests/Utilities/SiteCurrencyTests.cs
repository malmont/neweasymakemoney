using System.Globalization;
using Easymakemoney.Converters;
using Easymakemoney.Utilities;
using Xunit;

namespace Easymakemoney.Tests.Utilities;

[Collection("SiteCurrency")]
public class SiteCurrencyTests : IDisposable
{
    private readonly CultureInfo _originalCulture;

    public SiteCurrencyTests()
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
    public void FormatCents_WithCurrencyAndCulture_FormatsProperly()
    {
        SiteCurrency.SetCurrency("EUR");
        var cultureFr = new CultureInfo("fr-FR");

        var result = SiteCurrency.FormatCents(12345, cultureFr);

        Assert.Contains("123,45", result);
        Assert.Contains("€", result);
        Assert.DoesNotContain("$", result);
    }

    [Fact]
    public void FormatCents_SiteCurrencyIndependentOfDeviceCulture_EurWithUsCulture()
    {
        SiteCurrency.SetCurrency("EUR");
        var cultureUs = new CultureInfo("en-US");

        var result = SiteCurrency.FormatCents(12345, cultureUs);

        Assert.Contains("123.45", result);
        Assert.Contains("€", result);
        Assert.DoesNotContain("$", result);
    }

    [Fact]
    public void FormatCents_SiteCurrencyIndependentOfDeviceCulture_UsdWithFrCulture()
    {
        SiteCurrency.SetCurrency("USD");
        var cultureFr = new CultureInfo("fr-FR");

        var result = SiteCurrency.FormatCents(98765, cultureFr);

        Assert.Contains("987,65", result);
        Assert.Contains("$", result);
        Assert.DoesNotContain("€", result);
    }

    [Fact]
    public void FormatCents_UnknownIsoCode_FallsBackToIsoCode()
    {
        SiteCurrency.SetCurrency("XYZ");
        var cultureFr = new CultureInfo("fr-FR");

        var result = SiteCurrency.FormatCents(5000, cultureFr);

        Assert.Contains("50,00", result);
        Assert.Contains("XYZ", result);
    }

    [Fact]
    public void FormatCents_NoCurrencyCode_FormatsNumberWithoutSymbol()
    {
        SiteCurrency.Reset();
        var cultureFr = new CultureInfo("fr-FR");

        var result = SiteCurrency.FormatCents(12345, cultureFr);

        Assert.Equal("123,45", result);
        Assert.DoesNotContain("€", result);
        Assert.DoesNotContain("$", result);
    }

    [Fact]
    public void Reset_ClearsCurrentCurrency()
    {
        SiteCurrency.SetCurrency("EUR");
        Assert.Equal("EUR", SiteCurrency.CurrentCurrency);

        SiteCurrency.Reset();
        Assert.Null(SiteCurrency.CurrentCurrency);
    }

    [Fact]
    public void CurrencyConverter_ConvertsVariousNumericTypes()
    {
        SiteCurrency.SetCurrency("EUR");
        var converter = new CurrencyConverter();
        var cultureFr = new CultureInfo("fr-FR");

        var resLong = (string)converter.Convert((long)1500, typeof(string), null!, cultureFr);
        Assert.Contains("15,00", resLong);
        Assert.Contains("€", resLong);

        var resDouble = (string)converter.Convert((double)2550, typeof(string), null!, cultureFr);
        Assert.Contains("25,50", resDouble);
        Assert.Contains("€", resDouble);

        var resInt = (string)converter.Convert((int)3000, typeof(string), null!, cultureFr);
        Assert.Contains("30,00", resInt);
        Assert.Contains("€", resInt);

        var resDecimal = (string)converter.Convert((decimal)4500, typeof(string), null!, cultureFr);
        Assert.Contains("45,00", resDecimal);
        Assert.Contains("€", resDecimal);

        var resString = (string)converter.Convert("5500", typeof(string), null!, cultureFr);
        Assert.Contains("55,00", resString);
        Assert.Contains("€", resString);

        var resNull = converter.Convert(null!, typeof(string), null!, cultureFr);
        Assert.Equal(string.Empty, resNull);
    }

    [Theory]
    [InlineData("CAD", "$")]
    [InlineData("EUR", "€")]
    [InlineData("USD", "$")]
    public void GetCurrencySymbol_KnownCurrency_ContainsExpectedSymbol(string code, string expectedSubstring)
    {
        var symbol = SiteCurrency.GetCurrencySymbol(code);
        Assert.Contains(expectedSubstring, symbol);
    }

    [Fact]
    public void GetCurrencySymbol_UnknownIsoCode_ReturnsIsoCode()
    {
        var symbol = SiteCurrency.GetCurrencySymbol("XYZ");
        Assert.Equal("XYZ", symbol);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GetCurrencySymbol_NullOrEmpty_ReturnsEmptyString(string? code)
    {
        var symbol = SiteCurrency.GetCurrencySymbol(code!);
        Assert.Equal(string.Empty, symbol);
    }
}

