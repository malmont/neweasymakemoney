using System.Globalization;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.RepriseUseCase;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public sealed class RepriseViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public RepriseViewModelTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private static (RepriseViewModel ViewModel, FakeRepriseService Service, FakeDialogService Dialog) Create()
    {
        var service = new FakeRepriseService();
        var dialog = new FakeDialogService();
        var vm = new RepriseViewModel(new RepriseUseCase(service), dialog);
        return (vm, service, dialog);
    }

    private static RepriseSimulation SampleSimulation(params RepriseProduct[] products) => new()
    {
        periods = [new ReprisePeriod { name = "Octobre 2026" }],
        sales = new RepriseSales { revenue = 50_000, items = 5, unknownCostItems = 1 },
        products = products.ToList(),
        productsTotal = products.Length,
        investment = new RepriseInvestment { known = 10_000, estimated = 5_000 }
    };

    [Fact]
    public async Task Load_InactiveState_SimulatesWithoutWriting_AndPopulatesProducts()
    {
        var (vm, service, _) = Create();
        service.StateToReturn = new RepriseState { active = false };
        service.SimulationToReturn = SampleSimulation(new RepriseProduct
        {
            productId = 10,
            name = "Robe Lin",
            quantitySold = 3,
            revenue = 18_000,
            costSource = "estimated",
            unitCost = 2_000
        });

        await vm.LoadAsync();

        Assert.Equal(1, service.GetStateCalls);
        Assert.Single(service.SimulateCalls);
        Assert.Equal((12, null, 1, 100), service.SimulateCalls[0]);
        Assert.Empty(service.ApplyCalls);
        Assert.Equal(0, service.CancelCalls);
        Assert.False(vm.IsActive);
        Assert.True(vm.ShowSimulation);
        Assert.True(vm.HasSalesToResume);
        Assert.Single(vm.Products);
        Assert.Equal("Robe Lin", vm.Products[0].name);
    }

    [Fact]
    public async Task Load_ActiveState_PopulatesCommandes_WithoutSimulation()
    {
        var (vm, service, _) = Create();
        service.StateToReturn = new RepriseState
        {
            active = true,
            months = 12,
            commandes =
            [
                new RepriseCommande { periodName = "Octobre 2026", productCount = 4, estimatedCount = 1, investment = 12_500 }
            ]
        };

        await vm.LoadAsync();

        Assert.True(vm.IsActive);
        Assert.False(vm.ShowSimulation);
        Assert.Empty(service.SimulateCalls);
        Assert.Single(vm.Commandes);
        Assert.Equal("Octobre 2026", vm.Commandes[0].periodName);
    }

    [Fact]
    public async Task EditCost_PromptedAmount_UpdatesProductInCents_AndSendsInApply()
    {
        var (vm, service, dialog) = Create();
        var fromCommande = new RepriseProduct { productId = 1, name = "Sac", costSource = "commande", unitCost = 1_500 };
        var editable = new RepriseProduct { productId = 2, name = "Chemise", costSource = "estimated", unitCost = 1_000 };
        service.StateToReturn = new RepriseState { active = false };
        service.SimulationToReturn = SampleSimulation(fromCommande, editable);
        await vm.LoadAsync();

        // Coût provenant d'une commande fournisseur : non modifiable
        await vm.EditCostCommand.ExecuteAsync(fromCommande);
        Assert.Empty(dialog.PromptCalls);

        // Coût modifiable saisi à la main ("12,50" -> 1250 centimes)
        dialog.PromptResult = "12,50";
        await vm.EditCostCommand.ExecuteAsync(editable);
        Assert.Single(dialog.PromptCalls);
        Assert.Equal(1250L, editable.EditedCostCents);

        dialog.ConfirmResult = true;
        service.OnApply = () =>
        {
            service.StateToReturn = new RepriseState { active = true, months = 12 };
            return Task.CompletedTask;
        };

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Single(service.ApplyCalls);
        var sentProduct = Assert.Single(service.ApplyCalls[0].EditedProducts);
        Assert.Equal(2, sentProduct.productId);
        Assert.Equal(1250L, sentProduct.EditedCostCents);
    }

    [Fact]
    public async Task Apply_Declined_DoesNotSendRequest_And_Confirmed_AppliesAndReloads()
    {
        var (vm, service, dialog) = Create();
        service.StateToReturn = new RepriseState { active = false };
        service.SimulationToReturn = SampleSimulation();
        await vm.LoadAsync();

        dialog.ConfirmResult = false;
        await vm.ApplyCommand.ExecuteAsync(null);
        Assert.Empty(service.ApplyCalls);

        dialog.ConfirmResult = true;
        service.OnApply = () =>
        {
            service.StateToReturn = new RepriseState { active = true, months = 12 };
            return Task.CompletedTask;
        };
        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Single(service.ApplyCalls);
        Assert.Equal(2, service.GetStateCalls);
        Assert.Contains("Historique repris.", dialog.ShownInfos);
    }

    [Fact]
    public async Task Apply_ServerRefusesRepriseActive_ShowsErrorWithDisplayedMessage()
    {
        var (vm, service, dialog) = Create();
        dialog.ConfirmResult = true;

        // Cas 1 : code REPRISE_ACTIVE sans champ message -> texte de secours d'ApiError
        service.ApplyException = new ApiException(
            ApiError.FromHttpResponse("""{"code":"REPRISE_ACTIVE"}""", HttpStatusCode.Conflict, null),
            HttpStatusCode.Conflict);
        await vm.ApplyCommand.ExecuteAsync(null);

        var firstError = Assert.IsType<ApiException>(Assert.Single(dialog.ShownErrors));
        Assert.Equal("REPRISE_ACTIVE", firstError.ApiError?.Code);
        Assert.Equal("Une reprise d'historique est déjà en place : annulez-la avant d'en appliquer une autre.", firstError.Message);

        // Cas 2 : code REPRISE_ACTIVE avec message renvoyé par le serveur
        dialog.ShownErrors.Clear();
        service.ApplyException = new ApiException(
            ApiError.FromHttpResponse("""{"code":"REPRISE_ACTIVE","message":"Message serveur reprise active."}""", HttpStatusCode.Conflict, null),
            HttpStatusCode.Conflict);
        await vm.ApplyCommand.ExecuteAsync(null);

        var secondError = Assert.IsType<ApiException>(Assert.Single(dialog.ShownErrors));
        Assert.Equal("Message serveur reprise active.", secondError.Message);
        Assert.DoesNotContain("Historique repris.", dialog.ShownInfos);
    }

    [Fact]
    public async Task Cancel_DeclinedAndConfirmed_HandlesConfirmationAndRefresh()
    {
        var (vm, service, dialog) = Create();
        service.StateToReturn = new RepriseState { active = true, months = 12 };
        await vm.LoadAsync();

        dialog.ConfirmResult = false;
        await vm.CancelCommand.ExecuteAsync(null);
        Assert.Equal(0, service.CancelCalls);

        dialog.ConfirmResult = true;
        service.OnCancel = () =>
        {
            service.StateToReturn = new RepriseState { active = false };
            return Task.CompletedTask;
        };
        await vm.CancelCommand.ExecuteAsync(null);

        Assert.Equal(1, service.CancelCalls);
        Assert.Equal(2, service.GetStateCalls);
        Assert.Contains("Reprise annulée.", dialog.ShownInfos);
    }

    [Fact]
    public async Task Cancel_ServerRefusesRepriseNotFound_ShowsErrorWithDisplayedMessage()
    {
        var (vm, service, dialog) = Create();
        dialog.ConfirmResult = true;

        // Cas 1 : code REPRISE_NOT_FOUND sans champ message -> texte de secours d'ApiError
        service.CancelException = new ApiException(
            ApiError.FromHttpResponse("""{"code":"REPRISE_NOT_FOUND"}""", HttpStatusCode.NotFound, null),
            HttpStatusCode.NotFound);
        await vm.CancelCommand.ExecuteAsync(null);

        var firstError = Assert.IsType<ApiException>(Assert.Single(dialog.ShownErrors));
        Assert.Equal("REPRISE_NOT_FOUND", firstError.ApiError?.Code);
        Assert.Equal("Aucune reprise d'historique à annuler.", firstError.Message);

        // Cas 2 : code REPRISE_NOT_FOUND avec message renvoyé par le serveur
        dialog.ShownErrors.Clear();
        service.CancelException = new ApiException(
            ApiError.FromHttpResponse("""{"code":"REPRISE_NOT_FOUND","message":"Aucune reprise active sur ce site."}""", HttpStatusCode.NotFound, null),
            HttpStatusCode.NotFound);
        await vm.CancelCommand.ExecuteAsync(null);

        var secondError = Assert.IsType<ApiException>(Assert.Single(dialog.ShownErrors));
        Assert.Equal("Aucune reprise active sur ce site.", secondError.Message);
        Assert.DoesNotContain("Reprise annulée.", dialog.ShownInfos);
    }
}
