using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class TreasuryMovementViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly FakeHttpService _fakeHttp = new();
    private readonly FakeDialogService _dialogService = new();

    public TreasuryMovementViewModelTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private TreasuryMovementViewModel CreateViewModel()
    {
        var treasuryService = new TreasuryService(_fakeHttp);
        return new TreasuryMovementViewModel(treasuryService, _dialogService);
    }

    [Fact]
    public async Task Save_Validations_DoNotSendRequest()
    {
        var vm = CreateViewModel();

        // 1. Nom vide
        vm.Label = "";
        vm.AmountText = "1500";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Contains("Donnez un nom au mouvement", _dialogService.ShownInfos[0]);
        Assert.Empty(_fakeHttp.PostedAuthData);

        // 2. Montant nul ou invalide
        vm.Label = "Loyer";
        vm.AmountText = "0";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Contains("Saisissez un montant supérieur à zéro.", _dialogService.ShownInfos[1]);
        Assert.Empty(_fakeHttp.PostedAuthData);

        // 3. Date de fin avant début sur mouvement récurrent
        vm.AmountText = "1500";
        vm.HasEndDate = true;
        vm.StartDate = DateTime.Today;
        vm.EndDate = DateTime.Today.AddDays(-1);
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Contains("La date de fin doit suivre la date de début.", _dialogService.ShownInfos[2]);
        Assert.Empty(_fakeHttp.PostedAuthData);

        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task Save_DefaultCategory_FixedCostOmittedInSerializedBody()
    {
        var vm = CreateViewModel();

        // Loyer : catégorie 'rent' (frais fixe par défaut), sortie, mensuel
        vm.Label = "Loyer local";
        vm.AmountText = "1500";
        vm.SelectedCategory = vm.Categories.First(c => c.Code == "rent");
        vm.SelectedDirection = vm.Directions.First(d => d.Code == "out");
        vm.SelectedRecurrence = vm.Recurrences.First(r => r.Code == "monthly");

        // Le gérant n'a pas touché à la case : IsFixedCost est true par défaut, FixedCostChoice est null
        Assert.True(vm.IsFixedCost);
        Assert.Null(vm.FixedCostChoice);

        await vm.SaveCommand.ExecuteAsync(null);

        var (url, body) = Assert.Single(_fakeHttp.PostedAuthData);
        Assert.Equal("gestion/tresorerie/mouvements", url);

        var json = JsonConvert.SerializeObject(body);
        Assert.Contains("\"label\":\"Loyer local\"", json);
        Assert.Contains("\"amount\":150000", json);
        Assert.DoesNotContain("\"fixedCost\"", json);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task Save_FixedCostModifiedByManager_FixedCostIncludedInSerializedBody_AndRetainedWhenCategoryChanges()
    {
        var vm = CreateViewModel();

        // Prêt : catégorie 'loan' (pas un frais fixe par défaut)
        vm.SelectedCategory = vm.Categories.First(c => c.Code == "loan");
        Assert.False(vm.IsFixedCost);

        // Le gérant coche explicitement la case
        vm.IsFixedCost = true;
        Assert.True(vm.FixedCostChoice);

        // Précision Claude : vérifier qu'en remettant sur 'rent' sans toucher la case, la case garde le choix du gérant
        vm.SelectedCategory = vm.Categories.First(c => c.Code == "rent");
        Assert.True(vm.IsFixedCost);

        // Revenir sur 'loan' pour tester l'envoi avec fixedCost: true
        vm.SelectedCategory = vm.Categories.First(c => c.Code == "loan");
        Assert.True(vm.IsFixedCost);
        Assert.True(vm.FixedCostChoice);

        vm.Label = "Remboursement prêt";
        vm.AmountText = "1500";

        await vm.SaveCommand.ExecuteAsync(null);

        var (url, body) = Assert.Single(_fakeHttp.PostedAuthData);
        Assert.Equal("gestion/tresorerie/mouvements", url);

        var json = JsonConvert.SerializeObject(body);
        Assert.Contains("\"fixedCost\":true", json);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task Save_OneTimeMovement_FixedCostOmittedInSerializedBody()
    {
        var vm = CreateViewModel();

        vm.Label = "Dépense ponctuelle";
        vm.AmountText = "1500";
        vm.SelectedRecurrence = vm.Recurrences.First(r => r.Code == "none");

        Assert.False(vm.CanBeFixedCost);
        Assert.Null(vm.FixedCostChoice);

        await vm.SaveCommand.ExecuteAsync(null);

        var (url, body) = Assert.Single(_fakeHttp.PostedAuthData);
        Assert.Equal("gestion/tresorerie/mouvements", url);

        var json = JsonConvert.SerializeObject(body);
        Assert.DoesNotContain("\"fixedCost\"", json);
        Assert.Empty(_dialogService.ShownErrors);
    }
}

