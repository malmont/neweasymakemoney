using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>« Ma journée » : avoirs clients et cartes-cadeaux à part de l'encaissé (lot backend AVOIRS).</summary>
[Collection("SiteCurrency")]
public class DayStoreValueTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public DayStoreValueTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private static string Money(long cents) => SiteCurrency.FormatCents(cents, currencyCode: "CAD");

    [Fact]
    public void Movements_AndOutstanding_AreShownOnlyWhenNotZero()
    {
        var day = JsonConvert.DeserializeObject<DaySummary>("""
        { "currency": "CAD", "receipts": { "total": 0,
          "storeCreditIssued": { "amount": 3334, "count": 1 }, "paidByStoreCredit": { "amount": 0, "count": 0 },
          "giftCardRecredited": null, "paidByGiftCard": { "amount": 1000, "count": 1 },
          "outstandingStoreCredit": { "amount": 0, "count": 0, "asOf": "2026-11-15T00:30:00+00:00" },
          "outstandingGiftCards": { "amount": 12000, "count": 2, "asOf": "2026-11-15T00:30:00+00:00" } } }
        """)!;

        Assert.Equal($"Avoirs émis {Money(3334)} · payé par carte-cadeau {Money(1000)}", day.StoreValueText);
        Assert.Equal($"À ce jour : cartes-cadeaux non dépensées {Money(12000)}", day.OutstandingText);
    }

    [Fact]
    public void AllZeroOrAbsent_ShowsNothing()
    {
        var day = JsonConvert.DeserializeObject<DaySummary>("""{ "currency": "CAD", "receipts": { "total": 0 } }""")!;
        Assert.False(day.HasStoreValue);
        Assert.False(day.HasOutstanding);
        Assert.False(new DaySummary().HasStoreValue);
    }
}
