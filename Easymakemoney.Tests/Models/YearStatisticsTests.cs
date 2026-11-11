using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>L'année de l'entreprise et l'exercice (lots backend AN + EX).</summary>
[Collection("SiteCurrency")]
public class YearStatisticsTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public YearStatisticsTests()
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
    { "currency": "CAD", "year": 2026, "status": "in_progress", "today": "2026-11-11",
      "fiscalYear": { "startMonth": 1, "from": "2026-01-01", "to": "2026-12-31", "label": "2026" },
      "months": [
        { "month": 10, "year": 2026, "from": "2026-10-01", "to": "2026-10-31", "elapsed": true, "sales": 117070, "grossMargin": 51225, "fixedCosts": 0, "result": -20000,
          "otherRevenue": { "rentals": 0, "subscriptions": 0, "total": 0 }, "variableCosts": { "shipping": 1, "advertising": 2, "notes": 3, "trip": 4, "total": 10 } },
        { "month": 11, "year": 2026, "from": "2026-11-01", "to": "2026-11-30", "elapsed": "partial", "sales": 0, "grossMargin": 0, "result": 0 },
        { "month": 12, "year": 2026, "from": "2026-12-01", "to": "2026-12-31", "elapsed": false, "sales": null, "grossMargin": null, "result": null } ],
      "cumulative": [ { "month": 10, "from": "2026-10-01", "otherRevenue": 0, "variableCosts": 10, "result": -20000 } ],
      "lastYear": { "year": 2025, "label": "2025", "months": [ { "month": 10, "from": "2025-10-01", "result": 5000 }, { "month": 11, "from": "2025-11-01", "result": 0 }, { "month": 12, "from": "2025-12-01", "result": 1000 } ], "cumulative": [] },
      "periods": [ { "id": 3, "name": "Automne 2026", "start": "2026-09-01", "end": "2026-11-30", "status": "open", "started": true, "net": -44475, "netAfterFixed": -50000, "otherRevenue": 31900, "frozen": false, "partial": false } ],
      "totals": { "from": "2026-01-01", "to": "2026-11-11", "sales": 308571, "grossMargin": 125430, "otherRevenue": { "total": 31900 }, "variableCosts": { "shipping": 36800, "advertising": 0, "notes": 58900, "trip": 0, "total": 95700 }, "fixedCosts": 0, "resultBeforeOtherRevenue": 29730, "result": 61630 },
      "profitability": { "tauxMarque": 44.6,
        "projection": { "method": "pace", "result": -12000, "months": [ { "month": 12, "year": 2026, "from": "2026-12-01", "result": -32000, "sources": { "sales": "pace", "variableCosts": "budget" } } ] },
        "breakEven": { "sales": 350000, "reached": false },
        "gap": { "margin": 12000, "sales": 26906, "perRemainingMonth": { "margin": 9000, "sales": 20180, "remainingMonths": 1.3 } } },
      "levers": [ { "key": "price", "label": "Prix +5 %", "detail": "À quantités égales.", "effect": 8000, "coversGap": false, "percent": 5, "requiredPercent": 7.5 },
                  { "key": "worstPeriod", "label": "Automne 2026", "detail": "D'où vient l'écart.", "effect": 50000, "coversGap": true } ],
      "finished": null,
      "caveats": [ { "code": "PROJECTION_PACE", "text": "Estimation au rythme des 30 derniers jours." } ] }
    """;

    private static YearStatistics Read() => JsonConvert.DeserializeObject<YearStatistics>(Json)!;

    [Fact]
    public void Reads_ElapsedAsBooleanOrText()
    {
        var data = Read();
        Assert.Equal("true", data.months[0].elapsed);
        Assert.Equal("partial", data.months[1].elapsed);
        Assert.Equal("false", data.months[2].elapsed);
        Assert.Equal(10, data.cumulative[0].variableCosts!.total);
        Assert.Equal(0, data.cumulative[0].otherRevenue!.total);
    }

    [Fact]
    public void Result_Projection_BreakEven_Gap()
    {
        var data = Read();

        Assert.False(data.IsFinished);
        Assert.Contains("616", data.ResultText);
        Assert.True(data.HasProjection);
        Assert.Contains("rythme", data.ProjectionMethodText);
        Assert.Equal("44,6 %", data.TauxMarqueText);
        Assert.Contains("Pas encore atteint", data.BreakEvenText);
        Assert.True(data.HasGap);
        Assert.Contains("Il manque", data.GapText);
        Assert.Contains("par mois restant", data.GapText);
    }

    [Fact]
    public void Composition_AddsUp_WithTermsForEachLine()
    {
        var rows = Read().CompositionRows();
        Assert.Equal(new[] { "Ventes", "Marge", "Locations et abonnements", "Frais variables", "Frais fixes", "Résultat de l'entreprise" }, rows.Select(r => r.Title));
        Assert.True(rows[^1].IsTotal);
        Assert.StartsWith("−", rows[3].Value);
        Assert.Contains("voyages", Read().VariableCostsDetail);
    }

    [Fact]
    public void Chart_UsesProjectionForFutureMonths_AndLastYear()
    {
        var points = Read().ChartPoints("result");

        Assert.Equal(3, points.Count);
        Assert.Equal(-20000, points[0].Value);
        Assert.False(points[0].IsEstimate);
        Assert.Equal(5000, points[0].LastYear);
        Assert.False(points[1].IsEstimate); // mois en cours : réalisé
        Assert.True(points[2].IsEstimate);
        Assert.Equal(-32000, points[2].Value);
        Assert.EndsWith("(estimé)", points[2].Label);
    }

    [Fact]
    public void Levers_And_Periods()
    {
        var data = Read();
        var levers = data.LeverRows();
        Assert.Contains("sur l'exercice", levers[0].EffectText);
        Assert.Equal("Hausse qui comblerait l'écart : 7,5 %", levers[0].RequiredText);
        Assert.True(levers[0].IsPrice);
        Assert.Contains("sous zéro", levers[1].EffectText);
        Assert.True(levers[1].CoversGap);

        var period = Assert.Single(data.PeriodRows());
        Assert.Contains("181", period.ResultText); // −500 + 319 = −181
        Assert.True(period.IsLoss);
        Assert.Contains("après frais fixes, locations et abonnements", period.NoteText);
    }

    [Fact]
    public void Finished_ShowsBilanAndHindsight()
    {
        var data = JsonConvert.DeserializeObject<YearStatistics>("""
        { "currency": "CAD", "year": 2025, "status": "finished", "fiscalYear": { "startMonth": 4, "label": "2025-2026" }, "months": [], "cumulative": [],
          "finished": { "bilan": { "result": -100000, "profitable": false }, "bestMonths": [ { "from": "2025-12-01", "result": 5000 } ], "worstMonths": [],
            "hindsight": { "priceRequiredPercent": 4.2, "fixedCostsCutRequired": 100000, "salesRequired": 900000 } } }
        """)!;

        Assert.True(data.IsFinished);
        Assert.Equal("2025-2026", data.Label);
        Assert.Contains("en perte", data.FinishedSummary);
        Assert.Contains("décembre 2025", data.BestMonthsText);
        Assert.Contains("4,2 % plus hauts", data.HindsightText);
        Assert.False(data.HasProjection);
    }

    [Theory]
    [InlineData(1, "De janvier à décembre")]
    [InlineData(4, "D'avril à mars")]
    [InlineData(10, "D'octobre à septembre")]
    [InlineData(12, "De décembre à novembre")]
    public void FiscalYear_RangeText(int month, string expected) => Assert.Equal(expected, FiscalYearSettings.RangeText(month));

    [Fact]
    public void FiscalYear_Alignment()
    {
        Assert.Contains("coïncident", FiscalYearSettings.AlignmentText(3));
        Assert.Contains("à cheval", FiscalYearSettings.AlignmentText(1));
        Assert.Equal(12, FiscalYearSettings.Months.Count);
        Assert.Equal("Avril", FiscalYearSettings.Months[3].Label);
    }

    [Fact]
    public async Task Service_Routes()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new YearStatistics() };
        var service = new YearStatisticsService(fakeHttp);

        await service.GetAsync(null, null);
        await service.GetAsync(2025, 10);
        fakeHttp.ResultToReturn = new FiscalYearSetting();
        await service.GetFiscalYearAsync();
        await service.SaveFiscalYearAsync(4);

        Assert.Equal(new[] { "gestion/statistiques/annee?locale=fr", "gestion/statistiques/annee?year=2025&pricePercent=10&locale=fr", "gestion/reglages/exercice" }, fakeHttp.RequestedUrls);
        var (url, body) = Assert.Single(fakeHttp.PutData);
        Assert.Equal("gestion/reglages/exercice", url);
        Assert.Equal("{\"startMonth\":4}", JsonConvert.SerializeObject(body));
    }
}
