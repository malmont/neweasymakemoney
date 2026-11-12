using System.Globalization;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class PromotionViewModelsTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly FakePromotionService _promotionService = new();
    private readonly FakeDialogService _dialogService = new();

    public PromotionViewModelsTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private PromotionCreateViewModel CreateCreateViewModel() =>
        new(_promotionService, _dialogService);

    private PromotionListViewModel CreateListViewModel() =>
        new(_promotionService, _dialogService);

    [Fact]
    public void PromotionCreate_Init_FromProduct_Advice_OrCommande_SetsScopeAndCanChooseVariant()
    {
        var vm = CreateCreateViewModel();

        // 1. Initialisation depuis un article seul
        vm.Init(0, 0, 15, 20);
        Assert.Equal(15, vm.ProductId);
        Assert.Equal(0, vm.AdviceId);
        Assert.Equal(0, vm.CommandeId);
        Assert.True(vm.CanChooseVariant);
        Assert.Contains("Cet article", vm.ScopeText);

        // 2. Initialisation depuis un conseil du guide
        vm.Init(5, 0, 0, 25);
        Assert.Equal(5, vm.AdviceId);
        Assert.False(vm.CanChooseVariant);
        Assert.Contains("Articles du conseil du guide", vm.ScopeText);
        Assert.Equal("25", vm.PercentText);

        // 3. Initialisation depuis une commande fournisseur
        vm.Init(0, 8, 0, 15);
        Assert.Equal(8, vm.CommandeId);
        Assert.False(vm.CanChooseVariant);
        Assert.Contains("Articles de cette commande", vm.ScopeText);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("4")]     // < 5 %
    [InlineData("95")]    // > 90 %
    [InlineData("abc")]
    public async Task PromotionCreate_ComputePreview_InvalidPercent_ShowsInfoWithoutRequest(string invalidPercent)
    {
        var vm = CreateCreateViewModel();
        vm.Init(0, 0, 15, 20);
        vm.PercentText = invalidPercent;

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        Assert.Contains("La remise doit être entre 5 et 90 %.", _dialogService.ShownInfos);
        Assert.Empty(_promotionService.PreviewCalls);
        Assert.Null(vm.Preview);
    }

    [Fact]
    public async Task PromotionCreate_ComputePreview_ValidRequest_SendsCorrectScopeAndDates()
    {
        var today = DateTime.Today; // Toutes les dates partent de DateTime.Today
        var vm = CreateCreateViewModel();
        vm.Init(5, 0, 0, 20);
        vm.StartDate = today.AddDays(2);
        vm.EndDate = today.AddDays(16);

        _promotionService.PreviewResult = new PromotionPreview
        {
            previewHash = "h-valid",
            period = new PromotionPeriod
            {
                startDate = TreasuryDates.Iso(today.AddDays(2)),
                endDate = TreasuryDates.Iso(today.AddDays(16)),
                days = 14
            },
            rows = new List<PromotionPreviewRow>
            {
                new() { productId = 15, name = "Manteau", regularPrice = 2000, promoPrice = 1600, changed = true }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        var call = Assert.Single(_promotionService.PreviewCalls);
        Assert.Equal(5, call.AdviceId);
        Assert.Null(call.CommandeId);
        Assert.Null(call.ProductIds);
        Assert.Equal(20, call.Percent);
        Assert.Equal(today.AddDays(2), call.StartDate);
        Assert.Equal(today.AddDays(16), call.EndDate);

        Assert.True(vm.HasPreview);
        Assert.Single(vm.Rows);
        Assert.False(vm.AllowBelowCost);
        Assert.False(vm.AllowBigChange);
        Assert.False(vm.AllowReplace);
    }

    [Fact]
    public async Task PromotionCreate_InputsChange_ClearsPreview()
    {
        var today = DateTime.Today;
        var vm = CreateCreateViewModel();
        vm.Init(0, 0, 15, 20);

        _promotionService.PreviewResult = new PromotionPreview
        {
            previewHash = "h1",
            rows = new List<PromotionPreviewRow>
            {
                new() { productId = 15, regularPrice = 2000, promoPrice = 1600, changed = true }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);
        Assert.Single(vm.Rows);

        // 1. Changement de pourcentage
        vm.PercentText = "30";
        Assert.False(vm.HasPreview);
        Assert.Empty(vm.Rows);
        Assert.False(vm.CanApply);

        // Rechargement
        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // 2. Changement de date de début
        vm.StartDate = today.AddDays(3);
        Assert.False(vm.HasPreview);

        // Rechargement
        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // 3. Changement de date de fin
        vm.EndDate = today.AddDays(25);
        Assert.False(vm.HasPreview);

        // Rechargement
        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // 4. Changement d'arrondi
        vm.SelectedRounding = vm.Roundings[1];
        Assert.False(vm.HasPreview);
    }

    [Fact]
    public async Task PromotionCreate_Apply_WithoutApplicablePreview_DoesNothing()
    {
        var vm = CreateCreateViewModel();
        vm.Init(0, 0, 15, 20);

        // Ligne avec alerte bloquante BIG_CHANGE, non autorisée
        _promotionService.PreviewResult = new PromotionPreview
        {
            previewHash = "h-blocked",
            rows = new List<PromotionPreviewRow>
            {
                new() { productId = 15, regularPrice = 2000, promoPrice = 800, changed = true, alerts = new List<string> { "BIG_CHANGE" } }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);
        Assert.False(vm.CanApply); // WritableCount == 0

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Empty(_dialogService.ConfirmCalls);
        Assert.Empty(_promotionService.ApplyCalls);
    }

    [Fact]
    public async Task PromotionCreate_Apply_ConfirmationRefused_DoesNotSendRequest()
    {
        var vm = CreateCreateViewModel();
        vm.Init(0, 0, 15, 20);

        _promotionService.PreviewResult = new PromotionPreview
        {
            previewHash = "h-apply",
            rows = new List<PromotionPreviewRow>
            {
                new() { productId = 15, regularPrice = 2000, promoPrice = 1600, changed = true }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.CanApply);

        _dialogService.ConfirmResult = false; // Gérant refuse
        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Empty(_promotionService.ApplyCalls);
    }

    [Fact]
    public async Task PromotionCreate_Apply_Confirmed_SendsHashAndAgreements_ShowsConfirmation()
    {
        var vm = CreateCreateViewModel();
        vm.Init(0, 0, 15, 20);

        _promotionService.PreviewResult = new PromotionPreview
        {
            previewHash = "h-confirmed",
            period = new PromotionPeriod { startDate = TreasuryDates.Iso(DateTime.Today), endDate = TreasuryDates.Iso(DateTime.Today.AddDays(14)), days = 14 },
            rows = new List<PromotionPreviewRow>
            {
                new() { productId = 15, regularPrice = 2000, promoPrice = 1600, changed = true },
                new() { productId = 16, regularPrice = 3000, promoPrice = 2400, changed = true }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        // Cocher les 3 autorisations
        vm.AllowBelowCost = true;
        vm.AllowBigChange = true;
        vm.AllowReplace = true;

        _dialogService.ConfirmResult = true;
        _promotionService.ApplyResult = new PriceApplyResult { changed = 2 };

        await vm.ApplyCommand.ExecuteAsync(null);

        var applyCall = Assert.Single(_promotionService.ApplyCalls);
        Assert.Equal("h-confirmed", applyCall.PreviewHash);
        Assert.True(applyCall.AllowBelowCost);
        Assert.True(applyCall.AllowBigChange);
        Assert.True(applyCall.AllowReplace);

        Assert.Contains("Promotion créée sur 2 articles.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task PromotionCreate_Apply_409PricesChanged_ShowsInfoAndRecomputesPreview()
    {
        var vm = CreateCreateViewModel();
        vm.Init(0, 0, 15, 20);

        _promotionService.PreviewResult = new PromotionPreview
        {
            previewHash = "h-409",
            period = new PromotionPeriod { startDate = TreasuryDates.Iso(DateTime.Today), endDate = TreasuryDates.Iso(DateTime.Today.AddDays(14)), days = 14 },
            rows = new List<PromotionPreviewRow>
            {
                new() { productId = 15, regularPrice = 2000, promoPrice = 1600, changed = true }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.Single(_promotionService.PreviewCalls);

        var apiError = ApiError.FromHttpResponse("""{ "error": "PRICES_CHANGED" }""", HttpStatusCode.Conflict, null);
        _promotionService.ApplyException = new ApiException(apiError, HttpStatusCode.Conflict);
        _dialogService.ConfirmResult = true;

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Contains("Des prix ont changé depuis l'aperçu. Vérifiez le nouvel aperçu.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
        // ComputePreview est relancé automatiquement
        Assert.Equal(2, _promotionService.PreviewCalls.Count);
    }

    [Fact]
    public async Task PromotionCreate_Apply_OtherException_ShowsErrorDialogOnce()
    {
        var vm = CreateCreateViewModel();
        vm.Init(0, 0, 15, 20);

        _promotionService.PreviewResult = new PromotionPreview
        {
            previewHash = "h-err",
            period = new PromotionPeriod { startDate = TreasuryDates.Iso(DateTime.Today), endDate = TreasuryDates.Iso(DateTime.Today.AddDays(14)), days = 14 },
            rows = new List<PromotionPreviewRow>
            {
                new() { productId = 15, regularPrice = 2000, promoPrice = 1600, changed = true }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        _promotionService.ApplyException = new HttpRequestException("Connexion impossible");
        _dialogService.ConfirmResult = true;

        await vm.ApplyCommand.ExecuteAsync(null);

        var error = Assert.Single(_dialogService.ShownErrors);
        Assert.IsType<HttpRequestException>(error);
    }

    [Fact]
    public async Task PromotionCreate_OpenVariantPromotion_WithoutShell_ExecutesSafely()
    {
        var vm = CreateCreateViewModel();
        vm.Init(0, 0, 15, 20);
        Assert.True(vm.CanChooseVariant);

        // En environnement xUnit sans Shell, OpenVariantPromotion retourne sans planter
        var exception = await Record.ExceptionAsync(() => vm.OpenVariantPromotionCommand.ExecuteAsync(null));

        Assert.Null(exception);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task PromotionList_LoadAsync_FiltersActiveAndPlanned_AndHandlesError()
    {
        var vm = CreateListViewModel();
        _promotionService.PromotionsResult = new List<PromotionItem>
        {
            new() { batchId = 1, status = "active", name = "Polo" },
            new() { batchId = 2, status = "planned", name = "Chemise" },
            new() { batchId = 3, status = "ended", name = "T-shirt" },
            new() { batchId = 4, status = "cancelled", name = "Pull" }
        };

        await vm.LoadAsync();

        // Seuls active et planned sont conservés
        Assert.Equal(2, vm.Promotions.Count);
        Assert.Equal(new int?[] { 1, 2 }, vm.Promotions.Select(p => p.batchId));
        Assert.False(vm.IsEmpty);

        // Erreur réseau
        _promotionService.GetPromotionsException = new HttpRequestException("Erreur liste");
        await vm.LoadAsync();

        var error = Assert.Single(_dialogService.ShownErrors);
        Assert.IsType<HttpRequestException>(error);
    }

    [Fact]
    public async Task PromotionList_End_ConfirmationRefused_DoesNotSendRequest()
    {
        var vm = CreateListViewModel();
        var item = new PromotionItem { batchId = 5, status = "active" };

        _dialogService.ConfirmResult = false; // Gérant refuse
        await vm.EndCommand.ExecuteAsync(item);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Empty(_promotionService.EndCalls);
    }

    [Fact]
    public async Task PromotionList_End_Confirmed_DifferentiatesPlannedAndActiveMessages_AndReloads()
    {
        var vm = CreateListViewModel();
        var plannedItem = new PromotionItem { batchId = 10, status = "planned" };
        var activeItem = new PromotionItem { batchId = 20, status = "active" };

        _dialogService.ConfirmResult = true;

        // 1. Fin d'une promotion planifiée (à venir)
        await vm.EndCommand.ExecuteAsync(plannedItem);
        var plannedConfirm = Assert.Single(_dialogService.ConfirmCalls);
        Assert.Contains("Cette promotion n'a pas commencé : elle est annulée", plannedConfirm.Message);
        Assert.Equal((10, false), Assert.Single(_promotionService.EndCalls));

        // 2. Fin d'une promotion en cours
        await vm.EndCommand.ExecuteAsync(activeItem);
        var activeConfirm = _dialogService.ConfirmCalls.Last();
        Assert.Contains("La promotion s'arrête maintenant pour tous les articles", activeConfirm.Message);
        Assert.Equal((20, false), _promotionService.EndCalls.Last());

        // Rechargement effectué
        Assert.Equal(2, _promotionService.GetPromotionsCalls.Count);
    }

    [Fact]
    public async Task PromotionList_End_409PromotionEditedSince_PromptsPartialEndAndRetriesUntouched()
    {
        var vm = CreateListViewModel();
        var item = new PromotionItem { batchId = 5, status = "active" };

        var apiError = ApiError.FromHttpResponse("""{ "error": "PROMOTION_EDITED_SINCE" }""", HttpStatusCode.Conflict, null);
        _promotionService.EndException = new ApiException(apiError, HttpStatusCode.Conflict);
        _promotionService.EndExceptionOnlyInitial = true;

        _dialogService.ConfirmResult = true; // Confirme la fin initiale puis la fin partielle
        await vm.EndCommand.ExecuteAsync(item);

        // Deux confirmations demandées : initiale puis celle des articles retouchés
        Assert.Equal(2, _dialogService.ConfirmCalls.Count);
        Assert.Contains("Certains articles ont été modifiés depuis", _dialogService.ConfirmCalls.Last().Message);

        // La deuxième tentative envoie onlyUntouched: true
        Assert.Equal(2, _promotionService.EndCalls.Count);
        Assert.Equal((5, true), _promotionService.EndCalls.Last());
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task PromotionList_ShowAdminInfo_DisplaysInfoForAdminPromotions()
    {
        var vm = CreateListViewModel();

        // Promotion venant de l'administration du site (batchId == null)
        var adminItem = new PromotionItem { batchId = null, name = "Promo Admin" };
        Assert.True(adminItem.FromAdmin);

        await vm.ShowAdminInfoCommand.ExecuteAsync(adminItem);

        Assert.Contains("Cette promotion a été saisie dans l'administration du site : modifiez-la là-bas.", _dialogService.ShownInfos);

        // Promotion créée dans l'application (batchId != null) : n'affiche rien
        var appItem = new PromotionItem { batchId = 12, name = "Promo App" };
        Assert.False(appItem.FromAdmin);

        _dialogService.ShownInfos.Clear();
        await vm.ShowAdminInfoCommand.ExecuteAsync(appItem);

        Assert.Empty(_dialogService.ShownInfos);
    }
}

