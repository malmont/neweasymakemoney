using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Frais fixes : contrat FF1 (liste, drapeau du mouvement, résultat de période) et FF2 (simulateur, préparation).</summary>
[Collection("SiteCurrency")]
public class FixedCostsTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public FixedCostsTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private const string ListJson = """
    { "currency": "CAD", "monthlyTotal": 266667,
      "byCategory": [ { "category": "rent", "label": "Loyer", "monthly": 150000 }, { "category": "salaries", "label": "Salaires", "monthly": 216667 }, { "category": "tax", "label": "Taxes", "monthly": 0 } ],
      "items": [
        { "id": 7, "label": "Assurance", "category": "insurance", "categoryLabel": "Assurances", "amount": 120000, "recurrence": "yearly", "monthlyEquivalent": 10000, "startDate": "2026-01-01", "endDate": "2026-06-30", "active": false, "fixedCostDefault": true },
        { "id": 3, "label": "Loyer", "category": "rent", "categoryLabel": "Loyer", "amount": 150000, "recurrence": "monthly", "monthlyEquivalent": 150000, "startDate": "2026-01-01", "endDate": null, "active": true, "fixedCostDefault": true },
        { "id": 9, "label": "Internet", "category": "utilities", "categoryLabel": "Énergie et télécoms", "amount": 4500, "recurrence": "monthly", "monthlyEquivalent": 4500, "startDate": "2027-01-01", "endDate": null, "active": false, "fixedCostDefault": true },
        { "id": 4, "label": "Paie", "category": "salaries", "categoryLabel": "Salaires", "amount": 50000, "recurrence": "weekly", "monthlyEquivalent": 216667, "startDate": "2026-03-01", "endDate": "2027-03-01", "active": true, "fixedCostDefault": false }
      ] }
    """;

    private static readonly DateTime Today = new(2026, 11, 10);

    [Fact]
    public void List_SortsActiveThenUpcomingThenEnded_HeaviestFirst()
    {
        var list = JsonConvert.DeserializeObject<FixedCostList>(ListJson)!;

        var items = list.SortedItems(Today);

        Assert.Equal(new[] { "Paie", "Loyer", "Internet", "Assurance" }, items.Select(i => i.label));
        Assert.All(items, item => Assert.Equal("CAD", item.Currency));
        Assert.Equal(new[] { "Salaires", "Loyer" }, list.Categories().Select(c => c.label));
    }

    [Fact]
    public void Item_TextsShowMonthlyEquivalentAndState()
    {
        var items = JsonConvert.DeserializeObject<FixedCostList>(ListJson)!.SortedItems(Today);
        var pay = items[0];
        var rent = items[1];
        var internet = items[2];
        var insurance = items[3];

        Assert.Contains("chaque semaine, soit", pay.AmountText);
        Assert.EndsWith("par mois", pay.AmountText);
        Assert.Equal("Salaires · jusqu'au 01/03/2027 · marqué à la main", pay.StatusTextOn(Today));
        Assert.Equal("Loyer · depuis le 01/01/2026", rent.StatusTextOn(Today));
        Assert.Equal("Énergie et télécoms · commence le 01/01/2027", internet.StatusTextOn(Today));
        Assert.Equal("Assurances · terminé le 30/06/2026", insurance.StatusTextOn(Today));
        Assert.True(rent.CanDelete);
    }

    [Fact]
    public async Task Service_UsesFixedCostRoute()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new FixedCostList() };
        var service = new TreasuryService(fakeHttp);

        await service.GetFixedCostsAsync(all: true);
        await service.GetFixedCostsAsync(all: false);

        Assert.Equal(new[] { "gestion/frais-fixes?all=1&locale=fr", "gestion/frais-fixes?locale=fr" }, fakeHttp.RequestedUrls);
    }

    [Theory]
    [InlineData("rent", true)]
    [InlineData("salaries", true)]
    [InlineData("insurance", true)]
    [InlineData("subscription", true)]
    [InlineData("utilities", true)]
    [InlineData("loan", false)]
    [InlineData("tax", false)]
    [InlineData("other", false)]
    public void Rule_DefaultCategoriesMatchBackend(string category, bool expected) =>
        Assert.Equal(expected, FixedCostRule.IsDefault(category));

    [Fact]
    public void Rule_OnlyRecurringOutflows()
    {
        Assert.True(FixedCostRule.CanApply("out", "monthly"));
        Assert.False(FixedCostRule.CanApply("in", "monthly"));
        Assert.False(FixedCostRule.CanApply("out", "none"));
    }

    [Fact]
    public void MovementForm_FollowsCategoryUntilTouched_AndSendsOnlyAChoiceDifferentFromDefault()
    {
        var vm = new TreasuryMovementViewModel(new TreasuryService(new FakeHttpService()), new FakeDialogService());

        Assert.Equal("rent", vm.SelectedCategory?.Code);
        Assert.True(vm.IsFixedCost);
        Assert.Null(vm.FixedCostChoice);

        vm.SelectedCategory = vm.Categories.First(c => c.Code == "loan");
        Assert.False(vm.IsFixedCost);
        Assert.Null(vm.FixedCostChoice);

        vm.IsFixedCost = true;
        Assert.True(vm.FixedCostChoice);

        vm.SelectedCategory = vm.Categories.First(c => c.Code == "other");
        Assert.True(vm.IsFixedCost);
        Assert.True(vm.FixedCostChoice);

        vm.SelectedRecurrence = vm.Recurrences.First(r => r.Code == "none");
        Assert.False(vm.CanBeFixedCost);
        Assert.Null(vm.FixedCostChoice);
    }

    [Fact]
    public void MovementRequest_OmitsFixedCostWhenNull()
    {
        var withDefault = new TreasuryMovementRequest { label = "Loyer", category = "rent", amount = 1, startDate = "2026-12-01", recurrence = "monthly" };
        var withChoice = new TreasuryMovementRequest { label = "Prêt", category = "loan", amount = 1, startDate = "2026-12-01", recurrence = "monthly", fixedCost = true };

        Assert.DoesNotContain("fixedCost", JsonConvert.SerializeObject(withDefault));
        Assert.Contains("\"fixedCost\":true", JsonConvert.SerializeObject(withChoice));
    }

    [Fact]
    public void Movement_ReadsFixedCostFlags()
    {
        var movement = JsonConvert.DeserializeObject<TreasuryMovement>(
            """{ "id": 3, "label": "Loyer", "category": "rent", "categoryLabel": "Loyer", "direction": "out", "amount": 150000, "recurrence": "monthly", "nextDate": "2026-12-01", "fixedCost": true, "fixedCostDefault": true, "monthlyEquivalent": 150000 }""")!;

        Assert.True(movement.fixedCost);
        Assert.Equal(150000, movement.monthlyEquivalent);
        Assert.Equal("Loyer · Chaque mois · frais fixe · prochaine fois le 01/12/2026", movement.ScheduleText);
        Assert.Contains(TreasuryChoices.Categories, c => c.Code == "utilities" && c.Label == "Énergie et télécoms");
    }

    [Fact]
    public void PeriodResult_ShowsResultAfterFixedCostsAndOverlap()
    {
        var result = JsonConvert.DeserializeObject<PeriodResult>("""
        { "frozen": false, "sales": { "revenue": 1000000 }, "result": { "gross": 400000, "net": 300000, "netRate": 30, "netAfterFixed": 36932, "netAfterFixedRate": 3.7 },
          "fixedCosts": { "total": 263068, "byCategory": [ { "category": "rent", "label": "Loyer", "amount": 147945 } ], "days": 30,
            "caveats": [ { "code": "FIXED_COSTS_PRORATED", "text": "Au prorata." }, { "code": "FIXED_COSTS_MAY_OVERLAP_NOTES", "text": "Des notes ressemblent à des frais fixes.", "notes": [ "Loyer octobre", "Hydro" ] } ] } }
        """)!;

        Assert.True(result.HasFixedCosts);
        Assert.StartsWith("−", result.FixedCostsText);
        Assert.Contains("369", result.NetAfterFixedText);
        Assert.False(result.IsLossAfterFixed);
        Assert.True(result.HasNetAfterFixedRate);
        Assert.Contains("Loyer octobre, Hydro", result.FixedCostsOverlapText);
        Assert.False(result.FixedCostsNotComputed);
    }

    [Fact]
    public void PeriodResult_FrozenBeforeFF1_SaysNotComputed_AndHidesRows()
    {
        var result = JsonConvert.DeserializeObject<PeriodResult>("""{ "frozen": true, "result": { "gross": 1, "net": 1 } }""")!;

        Assert.False(result.HasFixedCosts);
        Assert.True(result.FixedCostsNotComputed);
        Assert.False(result.HasFixedCostsOverlap);
    }

    [Fact]
    public void Simulation_AddsAfterFixedRowsOnlyWhenThereAreFixedCosts()
    {
        var withFixed = JsonConvert.DeserializeObject<PeriodSimulation>("""
        { "currency": "CAD",
          "base": { "result": 23773, "breakEven": { "revenue": 6417, "reached": true }, "fixedCosts": 36000, "resultAfterFixed": -12227, "breakEvenAfterFixed": { "revenue": 72423, "reached": false } },
          "simulated": { "result": 30000, "fixedCosts": 36000, "resultAfterFixed": -6000, "breakEvenAfterFixed": { "revenue": 60000, "reached": false } } }
        """)!;

        var labels = withFixed.Rows().Select(r => r.Label).ToList();
        var point = labels.IndexOf("Point mort (ventes nécessaires)");
        Assert.Equal("Frais fixes (part de la période)", labels[point + 1]);
        Assert.Equal("Résultat après frais fixes", labels[point + 2]);
        Assert.Equal("Point mort après frais fixes", labels[point + 3]);
        var after = withFixed.Rows().Single(r => r.Label == "Résultat après frais fixes");
        Assert.True(after.IsTotal);
        Assert.Equal(1, after.Tone);
        Assert.NotEmpty(after.Help);

        var without = new PeriodSimulation().Rows().Select(r => r.Label);
        Assert.DoesNotContain("Résultat après frais fixes", without);
    }

    [Fact]
    public void Preparation_AddsAfterFixedRowsOnlyWhenThereAreFixedCosts()
    {
        var withFixed = JsonConvert.DeserializeObject<PreparationSimulation>("""
        { "currency": "CAD", "plan": { "result": 23773, "breakEven": { "revenue": 6417 }, "fixedCosts": 36000, "resultAfterFixed": -12227, "breakEvenAfterFixed": { "revenue": 72423, "reached": false } } }
        """)!;

        var rows = withFixed.Rows();
        Assert.Contains(rows, r => r.Label == "Frais fixes (part de la période)" && r.BaseText.Contains("360"));
        Assert.Contains(rows, r => r.Label == "Résultat après frais fixes" && r.IsTotal && r.BaseText.Contains("122"));
        Assert.Contains(rows, r => r.Label == "Point mort après frais fixes" && r.BaseText.Contains("724"));

        var without = new PreparationSimulation().Rows().Select(r => r.Label);
        Assert.DoesNotContain("Frais fixes (part de la période)", without);
    }
}
