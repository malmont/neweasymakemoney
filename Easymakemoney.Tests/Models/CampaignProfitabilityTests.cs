using System.Globalization;
using Easymakemoney.Models;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class CampaignProfitabilityTests
{
    private static readonly CultureInfo Fr = new("fr-FR");

    private const string CompleteJson = """
    {
        "campaign": { "id": 7, "name": "Hiver" }, "estimate": true, "method": "before_after", "currency": "EUR",
        "group": { "campaignIds": [7], "amount": 10000, "from": "2026-11-01", "to": "2026-11-10", "shared": false },
        "window": { "from": "2026-11-01", "to": "2026-11-10", "days": 10, "complete": true },
        "reference": { "from": "2026-10-22", "to": "2026-10-31", "days": 10 },
        "scope": { "kind": "all", "productIds": [] },
        "during": { "revenue": 150000, "grossMargin": 60000, "orders": 30, "items": 45, "unknownCostRevenue": 0 },
        "before": { "revenue": 100000, "grossMargin": 41500, "orders": 20, "items": 30, "unknownCostRevenue": 0 },
        "lastYear": null,
        "uplift": { "revenue": 50000, "grossMargin": 18500 },
        "gainPerDollar": { "revenue": 5.0, "grossMargin": 1.85 },
        "caveats": ["Peu de ventes avant la campagne (moins de 5 commandes)."]
    }
    """;

    private static CampaignProfitability Complete() => JsonConvert.DeserializeObject<CampaignProfitability>(CompleteJson)!;

    [Fact]
    public void Deserializes_ContractWithoutLastYear()
    {
        var data = Complete();

        Assert.True(data.estimate);
        Assert.Equal(10, data.window.days);
        Assert.True(data.window.complete);
        Assert.Null(data.lastYear);
        Assert.Equal(1.85, data.gainPerDollar!.grossMargin);
        Assert.Equal(18500, data.uplift.grossMargin);
        Assert.Single(data.caveats);
    }

    [Fact]
    public void Report_GoodGain_IsGreenWithTwoRowsPerMetric()
    {
        var report = new CampaignProfitabilityReport(Complete(), Fr);

        Assert.Equal(CampaignGainLevel.Good, report.Level);
        Assert.Equal(CampaignProfitabilityReport.GoodColor, report.GainColor);
        Assert.Contains("1,85", report.GainText);
        Assert.Contains("1,00", report.GainCaption);
        Assert.False(report.HasStatus);
        Assert.False(report.IsShared);
        Assert.False(report.HasLastYear);
        Assert.Equal(new[] { "Ventes", "Marge", "Commandes" }, report.Metrics.Select(m => m.Title));
        Assert.All(report.Metrics, m => Assert.Equal(new[] { "Avant", "Pendant" }, m.Rows.Select(r => r.Label)));
        Assert.Equal("20", report.Metrics[2].Rows[0].Value);
        Assert.Equal("30", report.Metrics[2].Rows[1].Value);
        Assert.StartsWith("+", report.MarginUpliftText);
        Assert.Contains("185,00", report.MarginUpliftText);
        Assert.Equal("Ventes mesurées : tout le magasin", report.ScopeText);
        Assert.Equal("Pendant : du 01/11/2026 au 10/11/2026 (10 jours)", report.WindowText);
    }

    [Fact]
    public void Report_CaveatsAreShownAsIs()
    {
        var report = new CampaignProfitabilityReport(Complete(), Fr);

        Assert.True(report.HasCaveats);
        Assert.Equal("Peu de ventes avant la campagne (moins de 5 commandes).", report.Caveats[0]);
        Assert.Equal("• Peu de ventes avant la campagne (moins de 5 commandes).", report.CaveatsText);
    }

    [Fact]
    public void Report_LastYearPresent_AddsAThirdRow()
    {
        var data = Complete();
        data.lastYear = new CampaignSalesMetrics { revenue = 90000, grossMargin = 30000, orders = 18 };

        var report = new CampaignProfitabilityReport(data, Fr);

        Assert.True(report.HasLastYear);
        Assert.All(report.Metrics, m => Assert.Equal(new[] { "Avant", "Pendant", "L'an dernier" }, m.Rows.Select(r => r.Label)));
        Assert.Equal("18", report.Metrics[2].Rows[2].Value);
    }

    [Theory]
    [InlineData(1.01, CampaignGainLevel.Good)]
    [InlineData(1.0, CampaignGainLevel.Weak)]
    [InlineData(0.4, CampaignGainLevel.Weak)]
    [InlineData(0.0, CampaignGainLevel.Bad)]
    [InlineData(-0.6, CampaignGainLevel.Bad)]
    public void LevelFor_FollowsThresholds(double gain, CampaignGainLevel expected)
    {
        Assert.Equal(expected, CampaignProfitabilityReport.LevelFor(gain));
    }

    [Fact]
    public void Report_ZeroGain_IsRed()
    {
        var data = Complete();
        data.gainPerDollar = new CampaignGainPerDollar { revenue = 0, grossMargin = 0 };
        data.uplift = new CampaignUplift();

        var report = new CampaignProfitabilityReport(data, Fr);

        Assert.Equal(CampaignGainLevel.Bad, report.Level);
        Assert.Equal(CampaignProfitabilityReport.BadColor, report.GainColor);
        Assert.Contains("0,00", report.GainText);
        Assert.DoesNotContain("+", report.MarginUpliftText);
    }

    [Fact]
    public void Report_NegativeUplift_HasMinusSign()
    {
        var data = Complete();
        data.uplift = new CampaignUplift { revenue = -2500, grossMargin = -1000 };
        data.gainPerDollar = new CampaignGainPerDollar { revenue = -0.25, grossMargin = -0.1 };

        var report = new CampaignProfitabilityReport(data, Fr);

        Assert.StartsWith("−", report.MarginUpliftText);
        Assert.Contains("10,00", report.MarginUpliftText);
        Assert.Equal(CampaignGainLevel.Bad, report.Level);
    }

    [Fact]
    public void Report_WeakGain_IsOrange()
    {
        var data = Complete();
        data.gainPerDollar = new CampaignGainPerDollar { revenue = 1.2, grossMargin = 0.45 };

        var report = new CampaignProfitabilityReport(data, Fr);

        Assert.Equal(CampaignProfitabilityReport.WeakColor, report.GainColor);
    }

    [Fact]
    public void Report_NullGain_IsNeutralWithADash()
    {
        var data = Complete();
        data.gainPerDollar = null;

        var report = new CampaignProfitabilityReport(data, Fr);

        Assert.Equal(CampaignGainLevel.Neutral, report.Level);
        Assert.Equal("—", report.GainText);
        Assert.False(report.HasRevenueGain);
        Assert.Contains("Montant nul", report.GainVerdict);
    }

    [Fact]
    public void Report_RunningCampaign_SaysSo()
    {
        var data = Complete();
        data.window = new CampaignWindow { from = "2026-11-01", to = "2026-11-09", days = 9, complete = false };

        var report = new CampaignProfitabilityReport(data, Fr);

        Assert.True(report.IsRunning);
        Assert.True(report.HasStatus);
        Assert.Contains("en cours", report.StatusText);
        Assert.Contains("09/11/2026", report.StatusText);
        // La mesure provisoire reste affichée.
        Assert.Equal(CampaignGainLevel.Good, report.Level);
    }

    [Fact]
    public void Report_NotStarted_IsNeutral()
    {
        var data = Complete();
        data.window = new CampaignWindow { from = "2026-12-01", to = "2026-12-01", days = 0, complete = false };
        data.gainPerDollar = new CampaignGainPerDollar();
        data.lastYear = null;

        var report = new CampaignProfitabilityReport(data, Fr);

        Assert.True(report.IsNotStarted);
        Assert.False(report.IsRunning);
        Assert.Contains("Pas encore commencée", report.StatusText);
        Assert.Equal(CampaignGainLevel.Neutral, report.Level);
        Assert.Equal(CampaignProfitabilityReport.NeutralColor, report.GainColor);
        Assert.Equal("—", report.GainText);
        Assert.Equal(string.Empty, report.WindowText);
    }

    [Fact]
    public void Report_SharedGroup_MentionsOtherCampaigns()
    {
        var data = Complete();
        data.group = new CampaignGroup { campaignIds = new() { 7, 8 }, amount = 25000, shared = true };
        data.scope = new CampaignScope { kind = "targeted", productIds = new() { 1, 2, 3 } };

        var report = new CampaignProfitabilityReport(data, Fr);

        Assert.True(report.IsShared);
        Assert.StartsWith("Mesurée avec d'autres campagnes", report.SharedText);
        Assert.Contains("250,00", report.SharedText);
        Assert.Equal("Ventes mesurées : 3 articles ciblés", report.ScopeText);
    }
}
