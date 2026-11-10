using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

[Collection("SiteCurrency")]
public class CampaignTests : IDisposable
{
    private readonly CultureInfo _originalCulture;

    public CampaignTests()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        SiteCurrency.Reset();
        CultureInfo.CurrentCulture = _originalCulture;
    }

    private const string ForeignJson = """
    {
        "id": 7, "name": "Hiver Facebook", "channel": "facebook",
        "startDate": "2026-11-01", "endDate": "2026-11-15",
        "amount": 12000, "currency": "EUR", "exchangeRate": 1.5, "amountSite": 18000, "siteCurrency": "CAD",
        "productIds": [3, 4], "commandeIds": [], "notes": "Code HIVER",
        "document": { "id": 55, "type": "note", "invoiceNumber": "FB-1", "invoiceDate": "2026-11-16" },
        "period": { "id": 2, "periodKey": "2026-11", "name": "Novembre 2026" },
        "createdAt": "2026-11-16T10:00:00+00:00"
    }
    """;

    [Fact]
    public void Campaign_DeserializesContract()
    {
        var campaign = JsonConvert.DeserializeObject<Campaign>(ForeignJson)!;

        Assert.Equal(7, campaign.id);
        Assert.Equal("2026-11-01", campaign.startDate);
        Assert.Equal(12000, campaign.amount);
        Assert.Equal(1.5, campaign.exchangeRate);
        Assert.Equal(18000, campaign.amountSite);
        Assert.Equal(new[] { 3, 4 }, campaign.productIds);
        Assert.Equal(55, campaign.document!.id);
        Assert.Equal("Novembre 2026", campaign.period!.name);
    }

    [Fact]
    public void Campaign_ForeignCurrency_ShowsItsOwnCurrencyThenSiteAmount()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        var campaign = JsonConvert.DeserializeObject<Campaign>(ForeignJson)!;

        Assert.True(campaign.IsForeign);
        Assert.Contains("120,00", campaign.AmountText);
        Assert.Contains("€", campaign.AmountText);
        Assert.True(campaign.HasAmountSite);
        Assert.Contains("180,00", campaign.AmountSiteText);
        Assert.Equal("Facebook", campaign.ChannelText);
        Assert.Equal("Du 01/11/2026 au 15/11/2026", campaign.DatesText);
        Assert.Equal("Période : Novembre 2026", campaign.PeriodText);
        Assert.Equal("2 articles ciblés", campaign.TargetText);
    }

    [Fact]
    public void Campaign_SiteCurrency_HasNoSecondAmount()
    {
        var campaign = new Campaign
        {
            amount = 5000, currency = "CAD", siteCurrency = "CAD", amountSite = 5000,
            startDate = "2026-12-24", endDate = "2026-12-24",
        };

        Assert.False(campaign.IsForeign);
        Assert.False(campaign.HasAmountSite);
        Assert.Equal("Le 24/12/2026", campaign.DatesTextFor(new CultureInfo("fr-CA")));
        Assert.Equal("Sans période", campaign.PeriodText);
        Assert.Equal("Tout le magasin", campaign.TargetText);
    }

    [Theory]
    [InlineData("facebook", "Facebook")]
    [InlineData("instagram", "Instagram")]
    [InlineData("google", "Google")]
    [InlineData("tiktok", "TikTok")]
    [InlineData("email", "E-mail")]
    [InlineData("print", "Imprimé")]
    [InlineData("radio", "Radio")]
    [InlineData("flyer", "Flyers")]
    [InlineData("influence", "Influence")]
    [InlineData("event", "Événement")]
    [InlineData("other", "Autre")]
    [InlineData("inconnu", "Autre")]
    [InlineData(null, "Autre")]
    public void Channels_HaveFrenchLabels(string? code, string label)
    {
        Assert.Equal(label, CampaignChannels.LabelFor(code));
    }

    [Fact]
    public void Channels_ListsTheElevenContractChannels()
    {
        Assert.Equal(11, CampaignChannels.All.Count);
        Assert.Equal(CampaignChannels.All.Count, CampaignChannels.All.Select(c => c.Code).Distinct().Count());
    }

    [Theory]
    [InlineData("Meta Platforms Ireland Ltd", "facebook")]
    [InlineData("Facebook", "facebook")]
    [InlineData("Instagram ads", "instagram")]
    [InlineData("Google Ireland", "google")]
    [InlineData("TikTok Pte", "tiktok")]
    [InlineData("Imprimerie Dupont", null)]
    [InlineData(null, null)]
    public void Channels_GuessFromMerchant(string? merchant, string? expected)
    {
        Assert.Equal(expected, CampaignChannels.Guess(merchant));
    }
}
