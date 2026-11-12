using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class VariantPromotionListViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly FakeVariantPromotionService _service = new();
    private readonly FakeDialogService _dialogService = new();

    public VariantPromotionListViewModelTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
        VariantPromotionListViewModel.PromotionsChanged = false;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
        VariantPromotionListViewModel.PromotionsChanged = false;
    }

    private VariantPromotionListViewModel CreateViewModel() =>
        new(_service, _dialogService);

    [Fact]
    public async Task InitializeAsync_LoadsOnce_UnlessPromotionsChanged()
    {
        var vm = CreateViewModel();
        _service.PromotionsResult = new List<VariantPromotionItem>
        {
            new() { id = 1, batchId = 10, name = "Polo", status = "active", startDate = TreasuryDates.Iso(DateTime.Today) }
        };

        // 1er appel : charge la liste
        await vm.InitializeAsync();
        Assert.Single(_service.GetPromotionsCalls);
        Assert.Single(vm.Items);
        Assert.False(vm.IsEmpty);

        // 2e appel : ne recharge pas
        await vm.InitializeAsync();
        Assert.Single(_service.GetPromotionsCalls);

        // Drapeau activé après création d'une promotion
        VariantPromotionListViewModel.PromotionsChanged = true;

        // 3e appel : recharge et remet le drapeau à faux
        await vm.InitializeAsync();
        Assert.Equal(2, _service.GetPromotionsCalls.Count);
        Assert.False(VariantPromotionListViewModel.PromotionsChanged);
    }

    [Fact]
    public async Task End_CannotEnd_DoesNothing()
    {
        var today = DateTime.Today; // Toutes les dates partent de DateTime.Today
        var vm = CreateViewModel();

        // Promotion active terminée ce soir (endedAt renseigné aujourd'hui)
        var endingToday = new VariantPromotionItem
        {
            id = 1,
            batchId = 10,
            status = "active",
            endedAt = today.ToString("yyyy-MM-ddTHH:mm:sszzz")
        };
        Assert.False(endingToday.CanEnd);

        await vm.EndCommand.ExecuteAsync(endingToday);

        Assert.Empty(_dialogService.ConfirmCalls);
        Assert.Empty(_service.EndCalls);

        // Promotion déjà terminée (status = "ended")
        var ended = new VariantPromotionItem
        {
            id = 2,
            batchId = 11,
            status = "ended"
        };
        Assert.False(ended.CanEnd);

        await vm.EndCommand.ExecuteAsync(ended);

        Assert.Empty(_dialogService.ConfirmCalls);
        Assert.Empty(_service.EndCalls);
    }

    [Fact]
    public async Task End_ConfirmationRefused_DoesNotSendRequest()
    {
        var today = DateTime.Today;
        var vm = CreateViewModel();

        var item = new VariantPromotionItem
        {
            id = 1,
            batchId = 10,
            status = "active",
            startDate = TreasuryDates.Iso(today)
        };
        Assert.True(item.CanEnd);

        _dialogService.ConfirmResult = false; // Gérant refuse
        await vm.EndCommand.ExecuteAsync(item);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Empty(_service.EndCalls);
    }

    [Fact]
    public async Task End_Confirmed_Upcoming_DifferentiatesMessageAndCallsEnd()
    {
        var today = DateTime.Today;
        var vm = CreateViewModel();

        var item = new VariantPromotionItem
        {
            id = 1,
            batchId = 20,
            name = "Polo",
            status = "upcoming",
            startDate = TreasuryDates.Iso(today.AddDays(5))
        };

        _dialogService.ConfirmResult = true;
        await vm.EndCommand.ExecuteAsync(item);

        var confirm = Assert.Single(_dialogService.ConfirmCalls);
        Assert.Contains("Elle n'a pas commencé : elle ne s'appliquera jamais.", confirm.Message);

        var endCall = Assert.Single(_service.EndCalls);
        Assert.Equal(20, endCall);
        // LoadAsync appelé pour recharger
        Assert.Single(_service.GetPromotionsCalls);
    }

    [Fact]
    public async Task End_Confirmed_Active_WithOtherVariants_MentionsOthersAndCallsEnd()
    {
        var today = DateTime.Today;
        var vm = CreateViewModel();

        // Précision 1 de Claude : charge d'abord la liste avec 3 promotions du même batchId
        _service.PromotionsResult = new List<VariantPromotionItem>
        {
            new() { id = 1, batchId = 42, name = "Polo", variantLabel = "S", status = "active", startDate = TreasuryDates.Iso(today) },
            new() { id = 2, batchId = 42, name = "Polo", variantLabel = "M", status = "active", startDate = TreasuryDates.Iso(today) },
            new() { id = 3, batchId = 42, name = "Polo", variantLabel = "L", status = "active", startDate = TreasuryDates.Iso(today) }
        };

        await vm.InitializeAsync();
        Assert.Equal(3, vm.Items.Count);

        _dialogService.ConfirmResult = true;
        await vm.EndCommand.ExecuteAsync(vm.Items[0]);

        var confirm = Assert.Single(_dialogService.ConfirmCalls);
        Assert.Contains("Elle s'arrête ce soir ; une vente d'aujourd'hui garde la remise.", confirm.Message);
        Assert.Contains("Les 2 autre(s) variante(s) créée(s) en même temps s'arrêtent aussi.", confirm.Message);

        var endCall = Assert.Single(_service.EndCalls);
        Assert.Equal(42, endCall);
        // Rechargé après résiliation
        Assert.Equal(2, _service.GetPromotionsCalls.Count);
    }

    [Fact]
    public async Task End_Confirmed_Active_WithoutOtherVariants_NoMentionOfOthers()
    {
        var today = DateTime.Today;
        var vm = CreateViewModel();

        // Précision 1 de Claude : 1 seule promotion pour ce batchId (aucune mention d'autres variantes)
        _service.PromotionsResult = new List<VariantPromotionItem>
        {
            new() { id = 10, batchId = 99, name = "Chemise", variantLabel = "XL", status = "active", startDate = TreasuryDates.Iso(today) }
        };

        await vm.InitializeAsync();
        Assert.Single(vm.Items);

        _dialogService.ConfirmResult = true;
        await vm.EndCommand.ExecuteAsync(vm.Items[0]);

        var confirm = Assert.Single(_dialogService.ConfirmCalls);
        Assert.Contains("Elle s'arrête ce soir ; une vente d'aujourd'hui garde la remise.", confirm.Message);
        Assert.DoesNotContain("autre(s) variante(s)", confirm.Message);

        var endCall = Assert.Single(_service.EndCalls);
        Assert.Equal(99, endCall);
        Assert.Equal(2, _service.GetPromotionsCalls.Count);
    }

    [Fact]
    public async Task End_Exception_ShowsErrorDialogAndReloads()
    {
        var today = DateTime.Today;
        var vm = CreateViewModel();

        var item = new VariantPromotionItem
        {
            id = 5,
            batchId = 77,
            status = "active",
            startDate = TreasuryDates.Iso(today)
        };

        _service.EndException = new HttpRequestException("Erreur réseau");
        _dialogService.ConfirmResult = true;

        await vm.EndCommand.ExecuteAsync(item);

        var error = Assert.Single(_dialogService.ShownErrors);
        Assert.IsType<HttpRequestException>(error);
        // Rechargement effectué même en cas d'erreur
        Assert.Single(_service.GetPromotionsCalls);
    }
}

