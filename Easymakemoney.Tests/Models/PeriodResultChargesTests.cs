using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Résultat de période, lot FIS-1 : charges financières des prêts et résultat après charges.</summary>
[Collection("SiteCurrency")]
public class PeriodResultChargesTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public PeriodResultChargesTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        SiteCurrency.Reset();
        SiteCurrency.SetCurrency("EUR");
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private static string Digits(string text) => new(text.Where(char.IsDigit).ToArray());

    [Fact]
    public void Charges_AreReadAndShownAfterFixedCosts()
    {
        var json = """
        {
            "frozen": false,
            "sales": { "revenue": 500000 },
            "result": { "gross": 250000, "net": 200000, "netRate": 40, "netAfterFixed": 150000, "netAfterFixedRate": 30,
                        "netAfterCharges": 146550, "netAfterChargesRate": 29.3 },
            "fixedCosts": { "total": 50000, "byCategory": [], "days": 90, "caveats": [] },
            "charges": {
                "financial": { "interest": 2950, "insurance": 300, "fees": 200, "total": 3450,
                               "loans": [ { "id": 12, "interest": 2950, "insurance": 300, "fees": 200, "total": 3450 } ] },
                "total": 3450,
                "caveats": [ { "code": "LOAN_ROUNDING", "text": "Arrondi au cent." }, "OTHER_CURRENCY_EXCLUDED" ]
            }
        }
        """;

        var pr = JsonConvert.DeserializeObject<PeriodResult>(json)!;

        Assert.Equal(3450, pr.charges!.total);
        Assert.Equal(2950, pr.charges.financial.interest);
        Assert.Equal(1, pr.charges.financial.LoanCount);
        Assert.Equal(new[] { "LOAN_ROUNDING", "OTHER_CURRENCY_EXCLUDED" }, pr.charges.Caveats().Select(c => c.code));
        Assert.Equal(146550, pr.result.netAfterCharges);
        Assert.True(pr.HasCharges);
        Assert.True(pr.HasNetAfterChargesRate);
        Assert.False(pr.ChargesNotComputed);
        Assert.StartsWith("− ", pr.ChargesText);
        Assert.Equal("3450", Digits(pr.ChargesText));
        Assert.Equal("146550", Digits(pr.NetAfterChargesText));
        Assert.Equal("29,3 % des ventes", pr.NetAfterChargesRateText);
        Assert.StartsWith("dont intérêts", pr.ChargesDetailText);

        // net et netAfterFixed sont inchangés.
        Assert.Equal(200000, pr.result.net);
        Assert.Equal(150000, pr.result.netAfterFixed);
        Assert.Equal("150000", Digits(pr.NetAfterFixedText));
    }

    [Fact]
    public void ClosedBeforeLoans_ShowsTheNoteAndNoCharges()
    {
        var json = """
        {
            "frozen": true,
            "result": { "gross": 250000, "net": 200000, "netAfterFixed": 150000, "netAfterCharges": null, "netAfterChargesRate": null },
            "fixedCosts": { "total": 50000, "byCategory": [], "days": 90, "caveats": [] },
            "charges": null,
            "chargesNote": "CLOSED_BEFORE_LOANS"
        }
        """;

        var pr = JsonConvert.DeserializeObject<PeriodResult>(json)!;

        Assert.Null(pr.charges);
        Assert.True(pr.ChargesNotComputed);
        Assert.False(pr.HasCharges);
        Assert.False(pr.HasNetAfterChargesRate);
        Assert.Equal("Période close avant les prêts : charges non calculées.", PeriodResult.ChargesNotComputedText);
        Assert.Equal(150000, pr.result.netAfterFixed);
    }

    [Fact]
    public void ZeroCharges_HideTheRows()
    {
        var json = """
        { "result": { "net": 1000, "netAfterFixed": 1000, "netAfterCharges": 1000 },
          "charges": { "financial": { "interest": 0, "insurance": 0, "fees": 0, "total": 0, "loans": [] }, "total": 0, "caveats": [] } }
        """;

        var pr = JsonConvert.DeserializeObject<PeriodResult>(json)!;

        Assert.False(pr.HasCharges);
        Assert.False(pr.ChargesNotComputed);
    }
}
