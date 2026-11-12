using System.Globalization;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class VariantPromotionViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly FakeVariantPromotionService _service = new();
    private readonly FakeDialogService _dialogService = new();

    public VariantPromotionViewModelTests()
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

    private VariantPromotionViewModel CreateViewModel() =>
        new(_service, _dialogService);

    [Fact]
    public void Init_SetsProductIdAndScopeText()
    {
        var vm = CreateViewModel();

        vm.Init(5, "Polo Marine");

        Assert.Equal(5, vm.ProductId);
        Assert.Contains("Polo Marine", vm.ScopeText);
        Assert.Contains("cochez les tailles ou couleurs à remiser", vm.ScopeText);
        Assert.Contains("sans cumul", vm.ScopeText);
    }

    [Fact]
    public void StartDate_AdjustsEndDateIfPastOrExceedsMax()
    {
        var today = DateTime.Today; // Toutes les dates partent de DateTime.Today
        var vm = CreateViewModel();

        vm.StartDate = today.AddDays(5);
        vm.EndDate = today.AddDays(20);

        // Si StartDate dépasse EndDate, EndDate est ajusté pour suivre StartDate
        vm.StartDate = today.AddDays(25);
        Assert.Equal(today.AddDays(25), vm.EndDate);

        // Si EndDate dépasse MaxEndDate lors d'un changement de StartDate, il est ramené à MaxEndDate
        vm.EndDate = today.AddDays(200);
        vm.StartDate = today.AddDays(26);
        Assert.Equal(vm.MaxEndDate, vm.EndDate);
    }

    [Fact]
    public async Task ClearPreview_WhenInputsChange()
    {
        var today = DateTime.Today;
        var vm = CreateViewModel();
        vm.Init(5, "Polo");

        _service.PreviewResult = new VariantPromotionPreview
        {
            previewHash = "h1",
            rows = new List<VariantPromotionRow>
            {
                new() { variantId = 1, changed = true, basePrice = 2000, promoPrice = 1600, alerts = new() { "BELOW_COST" } }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);
        Assert.NotEmpty(vm.Choices);
        Assert.True(vm.HasBelowCost);

        // 1. Changement de pourcentage efface l'aperçu
        vm.PercentText = "30";
        Assert.False(vm.HasPreview);
        Assert.Empty(vm.Choices);

        // Rechargement
        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // 2. Changement de date de début efface l'aperçu
        vm.StartDate = today.AddDays(2);
        Assert.False(vm.HasPreview);

        // Rechargement
        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // 3. Changement de date de fin efface l'aperçu
        vm.EndDate = today.AddDays(30);
        Assert.False(vm.HasPreview);

        // Rechargement
        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // 4. Changement d'arrondi efface l'aperçu
        vm.SelectedRounding = vm.Roundings[1];
        Assert.False(vm.HasPreview);
    }

    [Fact]
    public async Task Apply_WhenPreviewIsOff_ShowsInfoWithoutRequest()
    {
        var vm = CreateViewModel();
        vm.Init(5, "Polo");

        _service.PreviewResult = new VariantPromotionPreview
        {
            enabled = false,
            warnings = new List<string> { "VARIANT_PROMOTIONS_OFF" },
            previewHash = "h-off",
            rows = new List<VariantPromotionRow>
            {
                new() { variantId = 1, changed = true, basePrice = 2000, promoPrice = 1600 }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);
        Assert.True(vm.Preview!.IsOff);

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Contains(_dialogService.ShownInfos, msg => msg.Contains("Les promotions par variante ne sont pas encore activées sur ce site."));
        // Aucun nouvel aperçu et aucun appel à ApplyAsync
        Assert.Single(_service.PreviewCalls);
        Assert.Empty(_service.ApplyCalls);
    }

    [Fact]
    public async Task Apply_WhenNoVariantSelected_ShowsInfoWithoutRequest()
    {
        var vm = CreateViewModel();
        vm.Init(5, "Polo");

        _service.PreviewResult = new VariantPromotionPreview
        {
            enabled = true,
            previewHash = "h-valid",
            rows = new List<VariantPromotionRow>
            {
                new() { variantId = 1, changed = true, basePrice = 2000, promoPrice = 1600 }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // Décocher toutes les variantes
        foreach (var choice in vm.Choices)
            choice.IsSelected = false;

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Contains("Cochez au moins une variante à remiser.", _dialogService.ShownInfos);
        Assert.Single(_service.PreviewCalls);
        Assert.Empty(_service.ApplyCalls);
    }

    [Fact]
    public async Task Apply_WhenNoWritableVariantWithSettings_ShowsInfoWithoutApply()
    {
        var vm = CreateViewModel();
        vm.Init(5, "Polo");

        // Variante avec alerte bloquante BELOW_COST
        var preview = new VariantPromotionPreview
        {
            enabled = true,
            previewHash = "h-blocked",
            rows = new List<VariantPromotionRow>
            {
                new() { variantId = 10, changed = true, basePrice = 2000, promoPrice = 1000, alerts = new List<string> { "BELOW_COST" } }
            }
        };

        _service.PreviewResult = preview;
        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // Le second aperçu (des seules variantes cochées) renvoie toujours BELOW_COST
        // Mais AllowBelowCost est laissé à false dans le ViewModel
        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Contains("Aucune variante cochée ne peut être remisée avec ces réglages : voyez les alertes de l'aperçu.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ConfirmCalls);
        Assert.Empty(_service.ApplyCalls);
    }

    [Fact]
    public async Task Apply_ConfirmationRefused_DoesNotSendApplyRequest()
    {
        var vm = CreateViewModel();
        vm.Init(5, "Polo");

        _service.PreviewResult = new VariantPromotionPreview
        {
            enabled = true,
            previewHash = "h-ok",
            rows = new List<VariantPromotionRow>
            {
                new() { variantId = 10, changed = true, basePrice = 2000, promoPrice = 1600 }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        _dialogService.ConfirmResult = false; // Le gérant annule
        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Empty(_service.ApplyCalls);
        Assert.False(VariantPromotionListViewModel.PromotionsChanged);
    }

    [Fact]
    public async Task Apply_Confirmed_SendsAllCheckedAllowFlagsAndSetsPromotionsChanged()
    {
        var vm = CreateViewModel();
        vm.Init(5, "Polo");

        // Aperçu initial avec 3 variantes : 10, 20 et 30
        var initialPreview = new VariantPromotionPreview
        {
            enabled = true,
            previewHash = "h-initial",
            rows = new List<VariantPromotionRow>
            {
                new() { variantId = 10, changed = true, basePrice = 2000, promoPrice = 1600 },
                new() { variantId = 20, changed = true, basePrice = 2500, promoPrice = 2000 },
                new() { variantId = 30, changed = true, basePrice = 3000, promoPrice = 2400 }
            }
        };
        _service.PreviewResult = initialPreview;
        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.Equal(3, vm.Choices.Count);

        // On ne garde cochées que les variantes 10 et 30 (la variante 20 est décochée)
        vm.Choices[1].IsSelected = false;

        // Coche des 4 interrupteurs d'autorisation
        vm.AllowBelowCost = true;
        vm.AllowBigChange = true;
        vm.AllowReplace = true;
        vm.AllowAboveProductPromotion = true;

        // Configuration du second aperçu (des seules variantes cochées)
        var scopedPreview = new VariantPromotionPreview
        {
            enabled = true,
            previewHash = "h-scoped-10-30",
            period = new VariantPromotionPeriod { startDate = TreasuryDates.Iso(DateTime.Today), endDate = TreasuryDates.Iso(DateTime.Today.AddDays(14)), days = 14 },
            rows = new List<VariantPromotionRow>
            {
                new() { variantId = 10, changed = true, basePrice = 2000, promoPrice = 1600 },
                new() { variantId = 30, changed = true, basePrice = 3000, promoPrice = 2400 }
            }
        };
        _service.PreviewResult = scopedPreview;

        _dialogService.ConfirmResult = true;
        _service.ApplyResult = new VariantPromotionApplyResult
        {
            batch = new VariantPromotionBatch { id = 42 },
            skipped = new List<VariantPromotionSkipped>()
        };

        await vm.ApplyCommand.ExecuteAsync(null);

        // Précision 2 de Claude : vérification que le second aperçu ne porte que les variantes cochées
        Assert.Equal(2, _service.PreviewCalls.Count);
        var secondPreviewCall = _service.PreviewCalls.Last();
        Assert.Equal(new List<int> { 10, 30 }, secondPreviewCall.VariantIds);
        Assert.Null(secondPreviewCall.ProductIds);

        // Vérification de l'appel ApplyAsync
        var applyCall = Assert.Single(_service.ApplyCalls);
        Assert.Equal("h-scoped-10-30", applyCall.PreviewHash);
        Assert.Equal(new List<int> { 10, 30 }, applyCall.Request.VariantIds);
        Assert.True(applyCall.AllowBelowCost);
        Assert.True(applyCall.AllowBigChange);
        Assert.True(applyCall.AllowReplace);
        Assert.True(applyCall.AllowAboveProductPromotion);

        // Statut et dialogue
        Assert.True(VariantPromotionListViewModel.PromotionsChanged);
        Assert.Contains("Promotion créée.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task Apply_Confirmed_WithSkippedVariants_ShowsDifferentConfirmationMessage()
    {
        var vm = CreateViewModel();
        vm.Init(5, "Polo");

        _service.PreviewResult = new VariantPromotionPreview
        {
            enabled = true,
            previewHash = "h-partial",
            period = new VariantPromotionPeriod { startDate = TreasuryDates.Iso(DateTime.Today), endDate = TreasuryDates.Iso(DateTime.Today.AddDays(14)), days = 14 },
            rows = new List<VariantPromotionRow>
            {
                new() { variantId = 10, changed = true, basePrice = 2000, promoPrice = 1600 }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        _dialogService.ConfirmResult = true;
        _service.ApplyResult = new VariantPromotionApplyResult
        {
            batch = new VariantPromotionBatch { id = 43 },
            skipped = new List<VariantPromotionSkipped>
            {
                new() { variantId = 20, alerts = new List<string> { "BELOW_COST" } }
            }
        };

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Contains("Promotion créée. 1 variante(s) écartée(s) : voyez leurs alertes.", _dialogService.ShownInfos);
        Assert.True(VariantPromotionListViewModel.PromotionsChanged);
    }

    [Theory]
    [InlineData("PRICES_CHANGED", "Des prix ont changé depuis l'aperçu. Vérifiez le nouvel aperçu.")]
    [InlineData("NOTHING_TO_APPLY", "Rien à appliquer : voyez les alertes de l'aperçu.")]
    public async Task Apply_ApiErrors_409PricesChanged_And_409NothingToApply_ShowsInfoAndRecomputesPreview(string errorCode, string expectedMessage)
    {
        var vm = CreateViewModel();
        vm.Init(5, "Polo");

        _service.PreviewResult = new VariantPromotionPreview
        {
            enabled = true,
            previewHash = "h-retry",
            period = new VariantPromotionPeriod { startDate = TreasuryDates.Iso(DateTime.Today), endDate = TreasuryDates.Iso(DateTime.Today.AddDays(14)), days = 14 },
            rows = new List<VariantPromotionRow>
            {
                new() { variantId = 10, changed = true, basePrice = 2000, promoPrice = 1600 }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.Single(_service.PreviewCalls);

        var apiError = ApiError.FromHttpResponse($$"""{ "error": "{{errorCode}}" }""", HttpStatusCode.Conflict, null);
        _service.ApplyException = new ApiException(apiError, HttpStatusCode.Conflict);
        _dialogService.ConfirmResult = true;

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Contains(expectedMessage, _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
        // ComputePreview est relancé automatiquement (donc 3 appels à PreviewAsync : initial, avant Apply, et recompute)
        Assert.Equal(3, _service.PreviewCalls.Count);
    }

    [Fact]
    public async Task Apply_ApiError_VariantPromotionsOff_ShowsSpecificInfoMessage()
    {
        var vm = CreateViewModel();
        vm.Init(5, "Polo");

        _service.PreviewResult = new VariantPromotionPreview
        {
            enabled = true,
            previewHash = "h-off",
            period = new VariantPromotionPeriod { startDate = TreasuryDates.Iso(DateTime.Today), endDate = TreasuryDates.Iso(DateTime.Today.AddDays(14)), days = 14 },
            rows = new List<VariantPromotionRow>
            {
                new() { variantId = 10, changed = true, basePrice = 2000, promoPrice = 1600 }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        var apiError = ApiError.FromHttpResponse("""{ "error": "VARIANT_PROMOTIONS_OFF" }""", HttpStatusCode.Conflict, null);
        _service.ApplyException = new ApiException(apiError, HttpStatusCode.Conflict);
        _dialogService.ConfirmResult = true;

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Contains("Les promotions par variante ne sont pas activées sur ce site.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
        // Pas de recompute après VARIANT_PROMOTIONS_OFF
        Assert.Equal(2, _service.PreviewCalls.Count);
    }

    [Fact]
    public async Task Apply_OtherException_ShowsErrorDialogOnce()
    {
        var vm = CreateViewModel();
        vm.Init(5, "Polo");

        _service.PreviewResult = new VariantPromotionPreview
        {
            enabled = true,
            previewHash = "h-err",
            period = new VariantPromotionPeriod { startDate = TreasuryDates.Iso(DateTime.Today), endDate = TreasuryDates.Iso(DateTime.Today.AddDays(14)), days = 14 },
            rows = new List<VariantPromotionRow>
            {
                new() { variantId = 10, changed = true, basePrice = 2000, promoPrice = 1600 }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        _service.ApplyException = new HttpRequestException("Connexion impossible");
        _dialogService.ConfirmResult = true;

        await vm.ApplyCommand.ExecuteAsync(null);

        var error = Assert.Single(_dialogService.ShownErrors);
        Assert.IsType<HttpRequestException>(error);
    }
}
