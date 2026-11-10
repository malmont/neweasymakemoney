using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

[Collection("SiteCurrency")]
public class TreasuryTests
{
    private const string Json = """
    { "currency": "CAD", "settings": { "threshold": 50000, "includeEstimatedSales": true },
      "balance": { "id": 4, "date": "2026-11-08", "amount": 245030, "note": null, "ageDays": 2 },
      "estimatedDailyReceipts": { "amount": 12000, "windowDays": 30, "from": "2026-10-10", "to": "2026-11-09", "orders": 41, "estimated": true },
      "alertSeries": "projectionWithSales",
      "projection": { "days": [], "low": { "date": "2026-11-15", "amount": -10000 }, "low14": { "date": "2026-11-15", "amount": -10000 }, "belowThresholdFrom": "2026-11-12", "end": { "date": "2027-02-07", "amount": -90000 } },
      "projectionWithSales": {
        "days": [
          { "date": "2026-11-08", "in": 0, "out": 0, "balance": 245030, "opening": true },
          { "date": "2026-11-09", "in": 0, "out": 30000, "receipts": 11000, "receiptsEstimated": false, "balance": 226030 },
          { "date": "2026-11-10", "in": 0, "out": 0, "receipts": 12000, "receiptsEstimated": true, "balance": 238030 },
          { "date": "2026-11-12", "in": 5000, "out": 280000, "receipts": 12000, "receiptsEstimated": true, "balance": -24970 }
        ],
        "low": { "date": "2026-11-12", "amount": -24970 }, "low14": { "date": "2026-11-12", "amount": -24970 },
        "belowThresholdFrom": "2026-11-12", "end": { "date": "2027-02-07", "amount": 400000 }, "estimated": true, "receiptsTotal": 990000 },
      "status": "critical",
      "byCategory": [ { "category": "rent", "label": "Loyer", "in": 0, "out": 280000 } ],
      "caveats": [ { "code": "TAXES_INCLUDED", "text": "Les encaissements comprennent les taxes." }, { "code": "RECORDED_EXPENSES_INCLUDED", "text": "Vos dépenses saisies sont comptées." } ] }
    """;

    private static TreasuryOverview Read() => JsonConvert.DeserializeObject<TreasuryOverview>(Json)!;

    [Fact]
    public void Overview_UsesAlertSeriesChosenBySettings()
    {
        var overview = Read();

        Assert.True(overview.HasBalance);
        Assert.True(overview.UsesEstimatedSales);
        Assert.Same(overview.projectionWithSales, overview.AlertSeries);
        Assert.Equal("Trésorerie critique", overview.StatusText);
        Assert.Contains("12/11/2026", overview.Low14Text);
        Assert.Equal("Sous le seuil à partir du 12/11/2026", overview.BelowThresholdText);
        Assert.Contains("• Les encaissements comprennent les taxes.", overview.CaveatsText);
    }

    [Fact]
    public void Days_ReadInAndOutDespiteReservedWords()
    {
        var day = Read().projectionWithSales!.days[3];

        Assert.Equal(5000, day.In);
        Assert.Equal(280000, day.Out);
        Assert.True(day.receiptsEstimated);
        Assert.True(day.IsNegative);
    }

    [Fact]
    public void UpcomingDays_SkipOpeningPastAndQuietDays()
    {
        var upcoming = Read().UpcomingDays(new DateTime(2026, 11, 10));

        var day = Assert.Single(upcoming);
        Assert.Equal("2026-11-12", day.date);
        Assert.Equal("CAD", day.Currency);
    }

    [Fact]
    public void WithoutBalance_StatusAsksForOne()
    {
        var overview = JsonConvert.DeserializeObject<TreasuryOverview>(
            """{ "currency": "CAD", "settings": { "threshold": 0, "includeEstimatedSales": false }, "balance": null, "projection": null, "projectionWithSales": null, "status": "no_balance", "byCategory": [], "caveats": [] }""")!;

        Assert.False(overview.HasBalance);
        Assert.Null(overview.AlertSeries);
        Assert.Equal("Aucun solde saisi", overview.StatusText);
        Assert.Equal("—", overview.Low14Text);
    }

    [Fact]
    public void Movement_TextsShowDirectionAndSchedule()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
            var movement = new TreasuryMovement
            {
                id = 3, label = "Loyer", category = "rent", categoryLabel = "Loyer", direction = "out",
                amount = 120000, recurrence = "monthly", nextDate = "2026-12-01", Currency = "CAD",
            };

            Assert.StartsWith("−", movement.AmountText);
            Assert.Contains("1", movement.AmountText);
            Assert.Contains("$", movement.AmountText);
            Assert.Equal("Loyer · Chaque mois · prochaine fois le 01/12/2026", movement.ScheduleText);
            Assert.True(movement.CanDelete);
            Assert.False(new TreasuryMovement { id = null, grouped = true }.CanDelete);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            SiteCurrency.Reset();
        }
    }

    [Fact]
    public async Task Service_UsesContractRoutesAndBodies()
    {
        var fakeHttp = new FakeHttpService();
        var service = new TreasuryService(fakeHttp);

        await service.AddBalanceAsync(new DateTime(2026, 11, 10), -1500, "  découvert  ");
        await service.AddMovementAsync(new TreasuryMovementRequest { label = "Loyer", category = "rent", amount = 120000, startDate = "2026-12-01", recurrence = "monthly" });
        await service.UpdateSettingsAsync(null, true);
        await service.DeleteMovementAsync(3);

        Assert.Equal("gestion/tresorerie/soldes", fakeHttp.PostedAuthData[0].Url);
        Assert.Equal("{\"date\":\"2026-11-10\",\"amount\":-1500,\"note\":\"découvert\"}", JsonConvert.SerializeObject(fakeHttp.PostedAuthData[0].Data));
        Assert.Equal("gestion/tresorerie/mouvements", fakeHttp.PostedAuthData[1].Url);
        Assert.Equal(
            "{\"label\":\"Loyer\",\"category\":\"rent\",\"direction\":\"out\",\"amount\":120000,\"startDate\":\"2026-12-01\",\"recurrence\":\"monthly\"}",
            JsonConvert.SerializeObject(fakeHttp.PostedAuthData[1].Data));
        var (settingsUrl, settingsBody) = Assert.Single(fakeHttp.PutData);
        Assert.Equal("gestion/tresorerie/reglages", settingsUrl);
        Assert.Equal("{\"includeEstimatedSales\":true}", JsonConvert.SerializeObject(settingsBody));
        Assert.Equal("gestion/tresorerie/mouvements/3", Assert.Single(fakeHttp.DeletedUrls));
    }
}
