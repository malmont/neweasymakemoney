using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Statistiques des locations (lot backend LOC1).</summary>
[Collection("SiteCurrency")]
public class RentalStatisticsTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public RentalStatisticsTests()
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
    { "currency": "CAD", "range": { "type": "period", "from": "2026-09-01", "to": "2026-11-30", "days": 91 },
      "revenue": 5000, "revenueByRentalDate": 4200, "rentals": 3, "cancellations": 1,
      "products": [
        { "productId": 1, "name": "Kayak", "granularity": "hours", "unit": "hours", "capacity": 2, "hoursPerDay": 8, "occupancy": 12.5, "bookedTime": 16, "offeredTime": 128, "revenue": 2000, "revenueByRentalDate": 2000, "rentals": 2, "averageDuration": 8, "cancellations": 0 },
        { "productId": 2, "name": "Tente", "granularity": "days", "unit": "days", "capacity": 1, "occupancy": 85, "bookedTime": 6, "offeredTime": 7, "revenue": 3000, "revenueByRentalDate": 2200, "rentals": 1, "averageDuration": 6, "cancellations": 1 } ],
      "weekdays": [ { "weekday": 6, "label": "samedi", "occupancy": 90, "rentals": 2 }, { "weekday": 1, "label": "lundi", "occupancy": 5, "rentals": 0 } ],
      "highDays": [ { "weekday": 6, "label": "samedi", "occupancy": 90 } ], "lowDays": [ 1, 7 ],
      "thresholds": { "peak": 80, "low": 15 },
      "caveats": [ { "code": "REVENUE_AT_SALE_DATE", "text": "Revenu compté à la date de vente." } ] }
    """;

    private static RentalStatistics Read() => JsonConvert.DeserializeObject<RentalStatistics>(Json)!;

    [Fact]
    public void Summary_AndRange()
    {
        var data = Read();
        Assert.StartsWith("Période en cours : du 01/09/2026 au 30/11/2026", data.RangeText);
        Assert.Contains("50", data.RevenueText);
        Assert.Equal("3", data.RentalsText);
        Assert.Contains("• Revenu compté", data.CaveatsText);
    }

    [Fact]
    public void Products_SortedByOccupancy_WithTones()
    {
        var rows = Read().ProductRows();
        Assert.Equal("Tente", rows[0].Name);
        Assert.Equal(1, rows[0].Tone);
        Assert.Equal(0.85, rows[0].Occupancy, 3);
        Assert.Contains("1 annulée(s)", rows[0].DetailText);
        Assert.Contains("6 j", rows[0].DetailText);
        Assert.Equal(-1, rows[1].Tone);
        Assert.Equal("12,5 %", rows[1].OccupancyText);
        Assert.Contains("(2 en stock)", rows[1].TimeText);
    }

    [Fact]
    public void Weekdays_MondayFirst_AndHighLowDays()
    {
        var data = Read();
        var points = data.WeekdayPoints();
        Assert.Equal("1", points[0].Key);
        Assert.Equal("lun.", points[0].ShortLabel);
        Assert.Equal(90, points[1].Value);
        Assert.Contains("90 % d'occupation", points[1].Label);
        Assert.Equal("Jours pleins (≥ 80 %) : samedi (90 %)", data.HighDaysText);
        Assert.Equal("Jours creux (< 15 %) : lundi, dimanche", data.LowDaysText);
    }

    [Fact]
    public async Task Service_Query()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new RentalStatistics() };
        var service = new RentalStatisticsService(fakeHttp);

        await service.GetAsync(new RentalQuery());
        await service.GetAsync(new RentalQuery(PeriodId: 5));
        await service.GetAsync(new RentalQuery(From: new DateTime(2026, 10, 12), To: new DateTime(2026, 11, 10)));

        Assert.Equal(new[]
        {
            "gestion/locations/statistiques?locale=fr",
            "gestion/locations/statistiques?periodId=5&locale=fr",
            "gestion/locations/statistiques?from=2026-10-12&to=2026-11-10&locale=fr",
        }, fakeHttp.RequestedUrls);
    }
}
