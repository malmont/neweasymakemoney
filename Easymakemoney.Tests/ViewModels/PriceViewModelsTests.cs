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
public class PriceViewModelsTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly FakePriceService _priceService = new();
    private readonly FakeDialogService _dialogService = new();

    public PriceViewModelsTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private PriceChangeViewModel CreateChangeViewModel() =>
        new(_priceService, _dialogService);

    private PriceBatchListViewModel CreateBatchListViewModel() =>
        new(_priceService, _dialogService);

    [Fact]
    public void PriceChange_Modes_SetsLabelsAndPlaceholders()
    {
        var vm = CreateChangeViewModel();

        // 1. Mode pourcentage par défaut
        Assert.Equal("percent", vm.SelectedMode?.Code);
        Assert.Equal("Variation (en %, négatif pour baisser)", vm.ValueLabel);
        Assert.Equal("Ex. : 5", vm.ValuePlaceholder);

        // 2. Bascule en mode coefficient
        vm.SelectedMode = vm.Modes[1];
        Assert.Equal("coefficient", vm.SelectedMode.Code);
        Assert.Equal("Coefficient (prix = coût × coefficient)", vm.ValueLabel);
        Assert.Equal("Ex. : 2,2", vm.ValuePlaceholder);
    }

    [Theory]
    [InlineData("percent", "abc", "Saisissez un pourcentage (par exemple 5 ou -10).")]
    [InlineData("percent", "", "Saisissez un pourcentage (par exemple 5 ou -10).")]
    [InlineData("coefficient", "xyz", "Saisissez un coefficient (par exemple 2,2).")]
    [InlineData("coefficient", "   ", "Saisissez un coefficient (par exemple 2,2).")]
    public async Task PriceChange_ComputePreview_InvalidValue_ShowsModeSpecificMessageWithoutRequest(string mode, string invalidValue, string expectedMessage)
    {
        var vm = CreateChangeViewModel();
        vm.SelectedMode = vm.Modes.First(m => m.Code == mode);
        vm.ValueText = invalidValue;

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        Assert.Contains(expectedMessage, _dialogService.ShownInfos);
        Assert.Empty(_priceService.PreviewCalls);
        Assert.Null(vm.Preview);
    }

    [Fact]
    public async Task PriceChange_ComputePreview_SendsCorrectScopeModeAndRounding()
    {
        var vm = CreateChangeViewModel();
        // Initialisation depuis un conseil du guide
        vm.Init(7, 0, null);
        Assert.Equal(7, vm.AdviceId);

        vm.ValueText = "10";
        vm.SelectedRounding = vm.Roundings[1]; // ends_99

        _priceService.PreviewResult = new PricePreview
        {
            previewHash = "h-price",
            rows = new List<PricePreviewRow>
            {
                new() { productId = 1, name = "Robe", oldPrice = 1000, newPrice = 1100, changed = true }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        var call = Assert.Single(_priceService.PreviewCalls);
        Assert.Equal(7, call.AdviceId);
        Assert.Null(call.CommandeId);
        Assert.Null(call.ProductIds);
        Assert.Equal("percent", call.Mode);
        Assert.Equal(10.0, call.Value);
        Assert.Equal("ends_99", call.Rounding);

        Assert.True(vm.HasPreview);
        Assert.Single(vm.Rows);
        Assert.False(vm.AllowBelowCost);
        Assert.False(vm.AllowBigChange);
    }

    [Fact]
    public async Task PriceChange_InputsChange_ClearsPreview()
    {
        var vm = CreateChangeViewModel();
        vm.Init(0, 0, null, productId: 5);
        vm.ValueText = "5";

        _priceService.PreviewResult = new PricePreview
        {
            previewHash = "h-change",
            rows = new List<PricePreviewRow>
            {
                new() { productId = 5, oldPrice = 1000, newPrice = 1050, changed = true }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);
        Assert.Single(vm.Rows);
        Assert.True(vm.CanApply);

        // 1. Changement de valeur efface l'aperçu
        vm.ValueText = "8";
        Assert.False(vm.HasPreview);
        Assert.Empty(vm.Rows);
        Assert.False(vm.CanApply);

        // Rechargement
        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // 2. Changement de mode efface l'aperçu
        vm.SelectedMode = vm.Modes[1];
        Assert.False(vm.HasPreview);

        // Rechargement
        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // 3. Changement d'arrondi efface l'aperçu
        vm.SelectedRounding = vm.Roundings[2];
        Assert.False(vm.HasPreview);
    }

    [Fact]
    public async Task PriceChange_Apply_ConfirmationRefused_DoesNotSendRequest()
    {
        var vm = CreateChangeViewModel();
        vm.Init(0, 0, null, productId: 5);
        vm.ValueText = "5";

        _priceService.PreviewResult = new PricePreview
        {
            previewHash = "h-apply-refuse",
            rows = new List<PricePreviewRow>
            {
                new() { productId = 5, oldPrice = 1000, newPrice = 1050, changed = true }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.CanApply);

        _dialogService.ConfirmResult = false; // Gérant refuse
        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Empty(_priceService.ApplyCalls);
    }

    [Fact]
    public async Task PriceChange_Apply_Confirmed_SendsHashAndAgreements_ShowsConfirmation()
    {
        var vm = CreateChangeViewModel();
        vm.Init(0, 0, null, productId: 5);
        vm.ValueText = "5";

        _priceService.PreviewResult = new PricePreview
        {
            previewHash = "h-apply-ok",
            rows = new List<PricePreviewRow>
            {
                new() { productId = 5, oldPrice = 1000, newPrice = 1050, changed = true }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        vm.AllowBelowCost = true;
        vm.AllowBigChange = true;

        _dialogService.ConfirmResult = true;
        _priceService.ApplyResult = new PriceApplyResult { changed = 1 };

        await vm.ApplyCommand.ExecuteAsync(null);

        var applyCall = Assert.Single(_priceService.ApplyCalls);
        Assert.Equal("h-apply-ok", applyCall.PreviewHash);
        Assert.True(applyCall.AllowBelowCost);
        Assert.True(applyCall.AllowBigChange);

        Assert.Contains("1 prix changé. Annulation possible pendant 24 h.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task PriceChange_Apply_409PricesChanged_ShowsInfoAndRecomputesPreview()
    {
        var vm = CreateChangeViewModel();
        vm.Init(0, 0, null, productId: 5);
        vm.ValueText = "5";

        _priceService.PreviewResult = new PricePreview
        {
            previewHash = "h-409",
            rows = new List<PricePreviewRow>
            {
                new() { productId = 5, oldPrice = 1000, newPrice = 1050, changed = true }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.Single(_priceService.PreviewCalls);

        var apiError = ApiError.FromHttpResponse("""{ "error": "PRICES_CHANGED" }""", HttpStatusCode.Conflict, null);
        _priceService.ApplyException = new ApiException(apiError, HttpStatusCode.Conflict);
        _dialogService.ConfirmResult = true;

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Contains("Des prix ont changé depuis l'aperçu. Vérifiez le nouvel aperçu avant d'appliquer.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
        // Recalcul automatique de l'aperçu
        Assert.Equal(2, _priceService.PreviewCalls.Count);
    }

    [Fact]
    public async Task PriceChange_Apply_OtherException_ShowsErrorDialogOnce()
    {
        var vm = CreateChangeViewModel();
        vm.Init(0, 0, null, productId: 5);
        vm.ValueText = "5";

        _priceService.PreviewResult = new PricePreview
        {
            previewHash = "h-err",
            rows = new List<PricePreviewRow>
            {
                new() { productId = 5, oldPrice = 1000, newPrice = 1050, changed = true }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        _priceService.ApplyException = new HttpRequestException("Erreur réseau");
        _dialogService.ConfirmResult = true;

        await vm.ApplyCommand.ExecuteAsync(null);

        var error = Assert.Single(_dialogService.ShownErrors);
        Assert.IsType<HttpRequestException>(error);
    }

    [Fact]
    public async Task PriceBatchList_LoadAsync_PopulatesBatchesAndHandlesError()
    {
        var vm = CreateBatchListViewModel();
        _priceService.BatchesResult = new List<PriceBatch>
        {
            new() { id = 1, mode = "percent", value = 5, changed = 3 },
            new() { id = 2, mode = "coefficient", value = 2.2, changed = 1 }
        };

        await vm.LoadAsync();

        Assert.Equal(2, vm.Batches.Count);
        Assert.False(vm.IsEmpty);

        // Erreur réseau
        _priceService.GetBatchesException = new HttpRequestException("Erreur batches");
        await vm.LoadAsync();

        var error = Assert.Single(_dialogService.ShownErrors);
        Assert.IsType<HttpRequestException>(error);
    }

    [Fact]
    public async Task PriceBatchList_Undo_CannotUndo_DoesNothing()
    {
        var vm = CreateBatchListViewModel();
        // Lot non annulable
        var batch = new PriceBatch { id = 10, undoable = false, undoBlockedBy = "PRICE_EDITED_SINCE" };
        Assert.False(batch.CanUndo);

        await vm.UndoCommand.ExecuteAsync(batch);

        Assert.Empty(_dialogService.ConfirmCalls);
        Assert.Empty(_priceService.UndoCalls);
    }

    [Fact]
    public async Task PriceBatchList_Undo_ConfirmationRefused_DoesNotSendRequest()
    {
        var vm = CreateBatchListViewModel();
        var batch = new PriceBatch { id = 11, undoable = true, changed = 4 };
        Assert.True(batch.CanUndo);

        _dialogService.ConfirmResult = false; // Gérant refuse
        await vm.UndoCommand.ExecuteAsync(batch);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Empty(_priceService.UndoCalls);
    }

    [Fact]
    public async Task PriceBatchList_Undo_Confirmed_CallsUndoAndReloads()
    {
        var vm = CreateBatchListViewModel();
        var batch = new PriceBatch { id = 12, undoable = true, changed = 3 };

        _dialogService.ConfirmResult = true;
        _priceService.UndoResult = new PriceUndoResult { restored = 3, adviceRestored = true };

        await vm.UndoCommand.ExecuteAsync(batch);

        var undoCall = Assert.Single(_priceService.UndoCalls);
        Assert.Equal(12, undoCall.BatchId);
        Assert.False(undoCall.OnlyUntouched);

        Assert.Contains("3 prix remis à l'ancien. Le conseil du guide redevient à faire.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
        // Rechargement effectué
        Assert.Single(_priceService.GetBatchesCalls);
    }

    [Fact]
    public async Task PriceBatchList_Undo_409PriceEditedSince_PromptsPartialUndoAndRetriesUntouched()
    {
        var vm = CreateBatchListViewModel();
        var batch = new PriceBatch { id = 13, undoable = true, changed = 5 };

        var apiError = ApiError.FromHttpResponse("""{ "error": "PRICE_EDITED_SINCE" }""", HttpStatusCode.Conflict, null);
        _priceService.UndoException = new ApiException(apiError, HttpStatusCode.Conflict);
        _priceService.UndoExceptionOnlyInitial = true;

        _dialogService.ConfirmResult = true;
        _priceService.UndoResult = new PriceUndoResult { restored = 4, adviceRestored = false };

        await vm.UndoCommand.ExecuteAsync(batch);

        // Deux confirmations : initiale et partielle
        Assert.Equal(2, _dialogService.ConfirmCalls.Count);
        Assert.Contains("Certains prix ont été modifiés depuis ce changement", _dialogService.ConfirmCalls.Last().Message);

        // Deuxième appel avec onlyUntouched: true
        Assert.Equal(2, _priceService.UndoCalls.Count);
        Assert.Equal((13, true), _priceService.UndoCalls.Last());
        Assert.Empty(_dialogService.ShownErrors);
        Assert.Single(_priceService.GetBatchesCalls);
    }
}
