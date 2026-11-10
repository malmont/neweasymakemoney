using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

[Collection("SiteCurrency")]
public class TripTests
{
    [Fact]
    public void FollowUp_DescribesOverrunAndKeepsPurchasesApart()
    {
        var followUp = JsonConvert.DeserializeObject<TripFollowUp>("""
        { "currency": "CAD", "budgetScope": "expenses", "inProgress": true, "elapsedDays": 3,
          "days": [ { "date": "2026-11-08", "purchases": 0, "expenses": 0, "spent": 0, "budget": 10000 },
                    { "date": "2026-11-09", "purchases": 50000, "expenses": 22000, "spent": 22000, "budget": 10000 } ],
          "totals": { "purchases": 50000, "expenses": 22000, "spent": 22000, "budget": 70000, "remaining": 48000, "allowanceToDate": 30000 },
          "today": null, "averageDaily": 7333, "projectedTotal": 51333, "status": "over", "overBy": 12000 }
        """)!;

        Assert.StartsWith("Au-dessus du budget de", followUp.StatusText);
        Assert.Contains("frais du voyage", followUp.ScopeText);
        var day = Assert.Single(followUp.SpendingDays());
        Assert.True(day.IsOver);
        Assert.Contains("achats", day.DetailText);
        Assert.Equal(22000.0 / 70000, followUp.Progress, 3);
        Assert.False(followUp.HasToday);
    }

    [Fact]
    public void Trip_IsForeignOnlyWithRateAndOtherCurrency()
    {
        var local = new Trip { currency = "CAD", siteCurrency = "CAD", exchangeRate = null };
        var abroad = new Trip { currency = "EUR", siteCurrency = "CAD", exchangeRate = 1.48 };

        Assert.False(local.IsForeign);
        Assert.True(abroad.IsForeign);
        Assert.True(new Trip { startDate = "2026-11-08", endDate = "2026-11-14" }.IsCurrent(new DateTime(2026, 11, 14)));
    }

    [Fact]
    public async Task Service_SendsDailyBudgetAsNullWhenComputed()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new Trip() };
        var service = new TripService(fakeHttp);

        await service.CreateAsync(new TripRequest { name = "Salon", startDate = "2026-11-20", endDate = "2026-11-24", budgetTotal = 150000 });
        fakeHttp.ResultToReturn = new TripFollowUp();
        await service.GetFollowUpAsync(4);

        var (url, body) = Assert.Single(fakeHttp.PostedAuthData);
        Assert.Equal("gestion/voyages", url);
        Assert.Equal("{\"name\":\"Salon\",\"startDate\":\"2026-11-20\",\"endDate\":\"2026-11-24\",\"budgetTotal\":150000,\"budgetDaily\":null}",
            JsonConvert.SerializeObject(body));
        Assert.Equal("gestion/voyages/4/suivi", Assert.Single(fakeHttp.RequestedUrls));
    }

    [Fact]
    public void NewErrorCodes_HaveFrenchMessages()
    {
        Assert.Equal("La date de fin doit suivre la date de début.", ApiError.TranslateValidationCode("BEFORE_START"));
        Assert.Equal("Ces dates chevauchent un autre voyage.", ApiError.GetFallbackMessage("TRIP_OVERLAP", System.Net.HttpStatusCode.Conflict, null));
    }
}

public class NoteDeFraisTripTests
{
    [Fact]
    public void TripId_IsReadButNeverSentBack()
    {
        var note = JsonConvert.DeserializeObject<NoteDeFrais>("""{ "id": 5, "name": "Hôtel", "montant": 12000, "date": "2026-11-10", "tripId": 3 }""")!;

        Assert.True(note.HasTrip);
        var json = JsonConvert.SerializeObject(note);
        Assert.DoesNotContain("tripId", json);
        Assert.DoesNotContain("HasTrip", json);
    }

    [Fact]
    public async Task AttachNotes_UsesDedicatedRoute()
    {
        var fakeHttp = new FakeHttpService();
        await new TripService(fakeHttp).AttachNotesAsync(null, new[] { 5 });

        var (url, body) = Assert.Single(fakeHttp.PostedAuthData);
        Assert.Equal("gestion/voyages/rattacher", url);
        Assert.Equal("{\"tripId\":null,\"noteIds\":[5]}", JsonConvert.SerializeObject(body));
    }
}
