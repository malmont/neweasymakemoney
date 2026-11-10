using System.Globalization;
using Easymakemoney.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class CampaignRequestBuilderTests
{
    private static CampaignFormInput Input(string amount = "12,50", string? currency = null, string? site = "CAD", string? rate = null) => new()
    {
        Name = "  Soldes Facebook  ",
        Channel = "facebook",
        StartDate = new DateTime(2026, 11, 1),
        EndDate = new DateTime(2026, 11, 15),
        AmountText = amount,
        Currency = currency,
        SiteCurrency = site,
        ExchangeRateText = rate,
    };

    [Theory]
    [InlineData("12,50", 1250)]
    [InlineData("12.50", 1250)]
    [InlineData("1 234,56", 123456)]
    [InlineData("0", 0)]
    public void Build_ReadsAmountTextAsCents(string text, long expected)
    {
        var result = CampaignRequestBuilder.Build(Input(text));

        Assert.True(result.IsValid);
        Assert.Equal(expected, result.Body!["amount"]!.Value<long>());
    }

    [Fact]
    public void Build_Create_SendsContractFields()
    {
        var result = CampaignRequestBuilder.Build(Input());

        Assert.True(result.IsValid);
        var body = result.Body!;
        Assert.Equal("Soldes Facebook", body["name"]!.Value<string>());
        Assert.Equal("facebook", body["channel"]!.Value<string>());
        Assert.Equal("2026-11-01", body["startDate"]!.Value<string>());
        Assert.Equal("2026-11-15", body["endDate"]!.Value<string>());
        Assert.Equal("CAD", body["currency"]!.Value<string>());
        Assert.Null(body["exchangeRate"]);
        // Création sans ciblage ni notes : champs absents, la campagne compte pour tout le magasin.
        Assert.Null(body["productIds"]);
        Assert.Null(body["notes"]);
        Assert.Null(body["commandeIds"]);
        Assert.Null(body["as"]);
    }

    [Fact]
    public void Build_ForeignCurrencyWithRate_SendsCurrencyAndRate()
    {
        var result = CampaignRequestBuilder.Build(Input("100,00", currency: "usd", site: "CAD", rate: "1,3542"));

        Assert.True(result.IsValid);
        Assert.Equal(10000, result.Body!["amount"]!.Value<long>());
        Assert.Equal("USD", result.Body["currency"]!.Value<string>());
        Assert.Equal(1.3542, result.Body["exchangeRate"]!.Value<double>(), 6);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-1,2")]
    [InlineData("abc")]
    public void Build_ForeignCurrencyWithoutValidRate_IsRefused(string? rate)
    {
        var result = CampaignRequestBuilder.Build(Input(currency: "EUR", site: "CAD", rate: rate));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("taux de change"));
    }

    [Fact]
    public void Build_CurrencyEqualToSite_NeedsNoRate()
    {
        var result = CampaignRequestBuilder.Build(Input(currency: "CAD", site: "cad"));

        Assert.True(result.IsValid);
        Assert.Equal("CAD", result.Body!["currency"]!.Value<string>());
        Assert.Null(result.Body["exchangeRate"]);
    }

    [Fact]
    public void Build_UnknownSiteCurrency_AsksRateForAChosenCurrency()
    {
        Assert.True(CampaignRequestBuilder.NeedsExchangeRate("EUR", null));
        Assert.False(CampaignRequestBuilder.NeedsExchangeRate(null, null));

        var result = CampaignRequestBuilder.Build(Input(currency: null, site: null));
        Assert.True(result.IsValid);
        Assert.Null(result.Body!["currency"]);
    }

    [Fact]
    public void Build_InvalidInput_ListsEveryProblem()
    {
        var input = Input("douze");
        input.Name = "  ";
        input.Channel = "tv";
        input.EndDate = new DateTime(2026, 10, 31);

        var result = CampaignRequestBuilder.Build(input);

        Assert.False(result.IsValid);
        Assert.Null(result.Body);
        Assert.Equal(4, result.Errors.Count);
    }

    [Fact]
    public void Build_Update_ClearsTargetsAndNotes()
    {
        var result = CampaignRequestBuilder.Build(Input(), isUpdate: true);

        Assert.True(result.IsValid);
        Assert.Equal(JTokenType.Array, result.Body!["productIds"]!.Type);
        Assert.Empty((JArray)result.Body["productIds"]!);
        Assert.Equal(JTokenType.Null, result.Body["notes"]!.Type);
    }

    [Fact]
    public void Build_WithTargetsAndNotes_SendsThem()
    {
        var input = Input();
        input.ProductIds = new[] { 12, 12, 0, 7 };
        input.Notes = " Code promo HIVER ";

        var result = CampaignRequestBuilder.Build(input);

        Assert.Equal(new[] { 12, 7 }, result.Body!["productIds"]!.Values<int>().ToArray());
        Assert.Equal("Code promo HIVER", result.Body["notes"]!.Value<string>());
    }

    [Fact]
    public void BuildConfirmation_AddsAsCampaign()
    {
        var result = CampaignRequestBuilder.BuildConfirmation(Input("49,99", currency: "USD", site: "CAD", rate: "1.4"));

        Assert.True(result.IsValid);
        var body = result.Body!;
        Assert.Equal("as", body.Properties().First().Name);
        Assert.Equal("campaign", body["as"]!.Value<string>());
        Assert.Equal(4999, body["amount"]!.Value<long>());
        Assert.Equal("USD", body["currency"]!.Value<string>());
        Assert.Equal(1.4, body["exchangeRate"]!.Value<double>(), 6);
        Assert.Equal("Soldes Facebook", body["name"]!.Value<string>());
    }

    [Fact]
    public void BuildConfirmation_InvalidInput_HasNoBody()
    {
        var result = CampaignRequestBuilder.BuildConfirmation(Input("x"));

        Assert.False(result.IsValid);
        Assert.Null(result.Body);
    }

    [Theory]
    [InlineData("PERIOD_CLOSED")]
    [InlineData("OUT_OF_CALENDAR")]
    [InlineData("EXCHANGE_RATE_REQUIRED")]
    [InlineData("DOCUMENT_CONFIRMED")]
    public void MessageFor_KnownCodes_AreReadable(string code)
    {
        Assert.False(string.IsNullOrWhiteSpace(CampaignRequestBuilder.MessageFor(code)));
    }

    [Fact]
    public void DeleteMessageFor_PeriodClosed_ExplainsTheRefusal()
    {
        Assert.Contains("clôturée", CampaignRequestBuilder.DeleteMessageFor("PERIOD_CLOSED"));
        Assert.Null(CampaignRequestBuilder.DeleteMessageFor("VALIDATION_FAILED"));
    }

    [Fact]
    public void FormatAmountAndRate_FollowCulture()
    {
        var fr = new CultureInfo("fr-FR");
        Assert.Equal("12,50", CampaignRequestBuilder.FormatAmount(1250, fr));
        Assert.Equal("1,3542", CampaignRequestBuilder.FormatRate(1.3542, fr));
        Assert.Equal("12.50", CampaignRequestBuilder.FormatAmount(1250, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ExchangeRateLabel_NamesBothCurrencies()
    {
        Assert.Equal("Taux : 1 USD en CAD", CampaignRequestBuilder.ExchangeRateLabel("usd", "CAD"));
        Assert.Equal("Taux : 1 EUR en devise du site", CampaignRequestBuilder.ExchangeRateLabel("EUR", null));
    }
}
