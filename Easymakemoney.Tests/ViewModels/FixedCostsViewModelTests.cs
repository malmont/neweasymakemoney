using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class FixedCostsViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly FakeHttpService _fakeHttp = new();
    private readonly FakeDialogService _dialogService = new();

    public FixedCostsViewModelTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private FixedCostsViewModel CreateViewModel()
    {
        var treasuryService = new TreasuryService(_fakeHttp);
        return new FixedCostsViewModel(treasuryService, _dialogService);
    }

    private static FixedCostList CreateSampleFixedCosts()
    {
        var today = DateTime.Today;
        return new FixedCostList
        {
            currency = "CAD",
            monthlyTotal = 100000,
            byCategory = new List<FixedCostCategory>
            {
                new() { category = "rent", label = "Loyer", monthly = 100000 },
                new() { category = "insurance", label = "Assurance", monthly = 50000 }
            },
            items = new List<FixedCostItem>
            {
                new() { id = 1, label = "Loyer", category = "rent", amount = 100000, monthlyEquivalent = 100000, recurrence = "monthly", active = true, startDate = TreasuryDates.Iso(today.AddMonths(-2)) },
                new() { id = 2, label = "Assurance future", category = "insurance", amount = 50000, monthlyEquivalent = 50000, recurrence = "monthly", active = false, startDate = TreasuryDates.Iso(today.AddMonths(1)) },
                new() { id = 3, label = "Ancien contrat", category = "other", amount = 20000, monthlyEquivalent = 20000, recurrence = "monthly", active = false, startDate = TreasuryDates.Iso(today.AddMonths(-6)), endDate = TreasuryDates.Iso(today.AddMonths(-1)) }
            }
        };
    }

    [Fact]
    public async Task LoadAsync_RequestsAllWithLocaleFr_AndPopulatesSortedItemsAndCategories()
    {
        _fakeHttp.ResultToReturn = CreateSampleFixedCosts();
        var vm = CreateViewModel();

        await vm.LoadAsync();

        Assert.Equal("gestion/frais-fixes?all=1&locale=fr", Assert.Single(_fakeHttp.RequestedUrls));
        Assert.Equal(3, vm.Items.Count);
        // Tri : actif (1), à venir (2), terminé (3)
        Assert.Equal(1, vm.Items[0].id);
        Assert.Equal(2, vm.Items[1].id);
        Assert.Equal(3, vm.Items[2].id);

        Assert.NotEmpty(vm.Categories);
        Assert.False(vm.IsEmpty);
        Assert.Contains("$", vm.MonthlyTotalText);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task Delete_WhenRefused_DoesNotSendDelete()
    {
        _fakeHttp.ResultToReturn = CreateSampleFixedCosts();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        var item = vm.Items[0];
        _dialogService.ConfirmResult = false;

        await vm.DeleteCommand.ExecuteAsync(item);

        Assert.Empty(_fakeHttp.DeletedUrls);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task Delete_WhenAccepted_SendsDeleteAndReloads()
    {
        _fakeHttp.ResultToReturn = CreateSampleFixedCosts();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        var item = vm.Items[0]; // id = 1
        _dialogService.ConfirmResult = true;

        await vm.DeleteCommand.ExecuteAsync(item);

        Assert.Contains("gestion/tresorerie/mouvements/1", _fakeHttp.DeletedUrls);
        // Doit recharger après suppression : 2 appels à gestion/frais-fixes?all=1&locale=fr
        Assert.Equal(2, _fakeHttp.RequestedUrls.Count);
        Assert.Equal("gestion/frais-fixes?all=1&locale=fr", _fakeHttp.RequestedUrls[1]);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task Delete_WhenItemCannotBeDeleted_DoesNothing()
    {
        _fakeHttp.ResultToReturn = CreateSampleFixedCosts();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        // id = 0 : CanDelete == false
        var undeletableItem = new FixedCostItem { id = 0, label = "Groupé" };
        Assert.False(undeletableItem.CanDelete);

        await vm.DeleteCommand.ExecuteAsync(undeletableItem);

        Assert.Empty(_fakeHttp.DeletedUrls);
        Assert.Single(_fakeHttp.RequestedUrls); // Pas de reload
        Assert.Empty(_dialogService.ShownErrors);
    }
}
