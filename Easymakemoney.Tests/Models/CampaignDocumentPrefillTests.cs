using System.Globalization;
using Easymakemoney.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class CampaignDocumentPrefillTests
{
    private static readonly CultureInfo Fr = new("fr-FR");

    [Fact]
    public void Read_NoteExtraction_PrefillsTheCampaign()
    {
        // Désérialisé comme par HttpService : Newtonsoft fait de « 2026-11-03 » une date dans le JObject.
        var document = JsonConvert.DeserializeObject<GestionDocument>("""
        { "id": 9, "type": "note", "status": "extracted",
          "extraction": { "amount": 4999, "date": "2026-11-03", "currency": "usd", "merchant": "Meta Platforms Ireland",
                          "description": "Publicité", "confidence": { "overall": 0.5 }, "notes": ["Taxes non lues"] } }
        """)!;

        var prefill = CampaignDocumentPrefill.Read(document.extraction, "CAD", Fr);

        Assert.Equal("49,99", prefill.AmountText);
        Assert.Equal(new DateTime(2026, 11, 3), prefill.Date);
        Assert.Equal("Meta Platforms Ireland", prefill.Name);
        Assert.Equal("facebook", prefill.Channel);
        Assert.Equal("USD", prefill.Currency);
        Assert.Contains("Lecture incertaine : vérifiez chaque champ.", prefill.Remarks);
        Assert.Contains("Taxes non lues", prefill.Remarks);
    }

    [Fact]
    public void Read_SiteCurrency_IsNotForeign()
    {
        var extraction = JObject.Parse("""{ "amount": 1250, "date": "2026-11-03", "currency": "CAD", "merchant": "Imprimerie Dupont" }""");

        var prefill = CampaignDocumentPrefill.Read(extraction, "CAD", Fr);

        Assert.Null(prefill.Currency);
        Assert.Null(prefill.Channel);
        Assert.Equal("12,50", prefill.AmountText);
        Assert.Empty(prefill.Remarks);
    }

    [Fact]
    public void Read_EmptyExtraction_LeavesEverythingEmpty()
    {
        var prefill = CampaignDocumentPrefill.Read(null, "CAD", Fr);

        Assert.Null(prefill.AmountText);
        Assert.Null(prefill.Date);
        Assert.Null(prefill.Name);
        Assert.Null(prefill.Currency);
    }

    [Fact]
    public void Read_WithoutMerchant_UsesDescription()
    {
        var extraction = new JObject { ["description"] = "Annonce Google Ads", ["date"] = "2026-11-03" };

        var prefill = CampaignDocumentPrefill.Read(extraction, "CAD", Fr);

        Assert.Equal("Annonce Google Ads", prefill.Name);
        Assert.Equal("google", prefill.Channel);
        Assert.Equal(new DateTime(2026, 11, 3), prefill.Date);
    }
}
