using System.Globalization;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Statistiques;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class RentalGridViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly FakeRentalGridService _service = new();
    private readonly FakeDialogService _dialogService = new();

    public RentalGridViewModelTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private RentalGridViewModel CreateViewModel() =>
        new(_service, _dialogService);

    [Fact]
    public async Task InitializeAsync_LoadsGridsAndModificationsOnce()
    {
        var vm = CreateViewModel();
        _service.GridsResult = new List<RentalGrid>
        {
            new() { id = 1, name = "Nautique", rates = new RentalRates { day = 5000 } }
        };
        _service.ModificationsResult = new List<RentalGridModification>
        {
            new() { id = 10, undoUntil = DateTimeOffset.UtcNow.AddHours(12).ToString("o") }
        };

        await vm.InitializeAsync();
        // Deuxième appel : idempotence, ne doit pas recharger
        await vm.InitializeAsync();

        Assert.Equal(1, _service.GetGridsCalls);
        Assert.Single(_service.GetModificationsCalls);
        Assert.Single(vm.Grids);
        Assert.Single(vm.Modifications);
        Assert.False(vm.IsEmpty);
        Assert.True(vm.HasModifications);
    }

    [Fact]
    public async Task SelectGrid_ClearsPreviewAndUpdatesSelectionText()
    {
        var vm = CreateViewModel();
        var grid = new RentalGrid
        {
            id = 2,
            name = "Vélos",
            rates = new RentalRates { day = 3000 }
        };

        // Simuler un aperçu existant
        vm.SelectGridCommand.Execute(grid);
        _service.PreviewResult = new RentalGridPreview
        {
            previewHash = "hash1",
            changes = new List<RentalGridChange>
            {
                new() { type = "day", before = 3000, after = 3300, percent = 10 }
            }
        };
        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // Sélectionner une autre grille efface l'aperçu
        var otherGrid = new RentalGrid
        {
            id = 3,
            name = "Skis",
            rates = new RentalRates { day = 4500 }
        };
        vm.SelectGridCommand.Execute(otherGrid);

        Assert.False(vm.HasPreview);
        Assert.Empty(vm.ChangeLines);
        Assert.True(vm.HasSelection);
        Assert.Contains("Skis", vm.SelectionText);
    }

    [Fact]
    public async Task ComputePreview_NoGridSelected_ShowsInfoWithoutRequest()
    {
        var vm = CreateViewModel();
        // Aucune grille sélectionnée

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        Assert.Contains("Choisissez d'abord une grille.", _dialogService.ShownInfos);
        Assert.Empty(_service.PreviewCalls);
        Assert.Null(vm.Preview);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-60")] // > 50 % de baisse
    [InlineData("250")] // > 200 % de hausse
    [InlineData("12.5")]
    [InlineData("abc")]
    public async Task ComputePreview_InvalidPercent_ShowsInfoWithoutRequest(string invalidPercent)
    {
        var vm = CreateViewModel();
        vm.SelectGridCommand.Execute(new RentalGrid { id = 1, name = "Grille 1" });
        vm.PercentText = invalidPercent;

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        Assert.NotEmpty(_dialogService.ShownInfos);
        Assert.Empty(_service.PreviewCalls);
        Assert.Null(vm.Preview);
    }

    [Fact]
    public async Task ComputePreview_DecreaseDirection_SendsNegativePercent()
    {
        var vm = CreateViewModel();
        vm.SelectGridCommand.Execute(new RentalGrid { id = 5, name = "Grille Baisse" });

        vm.SetDirectionCommand.Execute("down");
        vm.PercentText = "15";

        _service.PreviewResult = new RentalGridPreview
        {
            previewHash = "hash-down",
            changes = new List<RentalGridChange>
            {
                new() { type = "day", before = 2000, after = 1700, percent = -15 }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        var call = Assert.Single(_service.PreviewCalls);
        Assert.Equal(5, call.RentalPackId);
        Assert.Equal(-15, call.Percent);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ComputePreview_RoundToDollar_OptionReflectedInRequest(bool roundToDollar)
    {
        var vm = CreateViewModel();
        vm.SelectGridCommand.Execute(new RentalGrid { id = 1, name = "Grille 1" });
        vm.PercentText = "10";
        vm.RoundToDollar = roundToDollar;

        _service.PreviewResult = new RentalGridPreview
        {
            previewHash = "hash",
            changes = new List<RentalGridChange> { new() { type = "day", before = 1000, after = 1100, percent = 10 } }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        var call = Assert.Single(_service.PreviewCalls);
        Assert.Equal(roundToDollar, call.RoundToDollar);
    }

    [Fact]
    public async Task InputsChange_ClearsPreview()
    {
        var vm = CreateViewModel();
        vm.SelectGridCommand.Execute(new RentalGrid { id = 1, name = "Grille 1" });
        vm.PercentText = "10";

        _service.PreviewResult = new RentalGridPreview
        {
            previewHash = "h1",
            changes = new List<RentalGridChange> { new() { type = "day", before = 1000, after = 1100, percent = 10 } }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // Changement de pourcentage
        vm.PercentText = "20";
        Assert.False(vm.HasPreview);

        // Remise aperçu
        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // Changement de direction
        vm.SetDirectionCommand.Execute("down");
        Assert.False(vm.HasPreview);

        // Remise aperçu
        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview);

        // Changement d'arrondi
        vm.RoundToDollar = !vm.RoundToDollar;
        Assert.False(vm.HasPreview);
    }

    [Fact]
    public async Task Apply_SharedGrid_RequiresAllowSharedAndConfirmsWithProductNames()
    {
        var vm = CreateViewModel();
        var grid = new RentalGrid { id = 10, name = "Grille commune" };
        vm.SelectGridCommand.Execute(grid);
        vm.PercentText = "10";

        _service.PreviewResult = new RentalGridPreview
        {
            previewHash = "hash-shared",
            shared = true,
            products = new List<RentalGridProduct>
            {
                new() { productId = 1, name = "Kayak" },
                new() { productId = 2, name = "Pédalo" }
            },
            changes = new List<RentalGridChange>
            {
                new() { type = "day", before = 5000, after = 5500, percent = 10 }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.CanApply);

        _dialogService.ConfirmResult = true;
        await vm.ApplyCommand.ExecuteAsync(null);

        var confirm = Assert.Single(_dialogService.ConfirmCalls);
        // La confirmation cite les articles de la grille partagée
        Assert.Contains("Kayak", confirm.Message);
        Assert.Contains("Pédalo", confirm.Message);

        var applyCall = Assert.Single(_service.ApplyCalls);
        Assert.Equal("hash-shared", applyCall.PreviewHash);
        Assert.True(applyCall.AllowShared);
    }

    [Fact]
    public async Task Apply_NonSharedGrid_SendsAllowSharedFalse()
    {
        var vm = CreateViewModel();
        vm.SelectGridCommand.Execute(new RentalGrid { id = 11, name = "Grille seule" });
        vm.PercentText = "10";

        _service.PreviewResult = new RentalGridPreview
        {
            previewHash = "hash-solo",
            shared = false,
            products = new List<RentalGridProduct>
            {
                new() { productId = 1, name = "Kayak" }
            },
            changes = new List<RentalGridChange>
            {
                new() { type = "day", before = 5000, after = 5500, percent = 10 }
            }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.True(vm.CanApply);

        _dialogService.ConfirmResult = true;
        await vm.ApplyCommand.ExecuteAsync(null);

        var applyCall = Assert.Single(_service.ApplyCalls);
        Assert.Equal("hash-solo", applyCall.PreviewHash);
        Assert.False(applyCall.AllowShared);
    }

    [Fact]
    public async Task Apply_ConfirmationRefused_DoesNotSendRequest()
    {
        var vm = CreateViewModel();
        vm.SelectGridCommand.Execute(new RentalGrid { id = 11, name = "Grille" });

        _service.PreviewResult = new RentalGridPreview
        {
            previewHash = "hash1",
            changes = new List<RentalGridChange> { new() { type = "day", before = 1000, after = 1100, percent = 10 } }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        _dialogService.ConfirmResult = false;
        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Empty(_service.ApplyCalls);
    }

    [Fact]
    public async Task Apply_Success_ClearsSelectionAndReloads()
    {
        var vm = CreateViewModel();
        vm.SelectGridCommand.Execute(new RentalGrid { id = 11, name = "Grille" });

        _service.PreviewResult = new RentalGridPreview
        {
            previewHash = "hash1",
            changes = new List<RentalGridChange> { new() { type = "day", before = 1000, after = 1100, percent = 10 } }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        _dialogService.ConfirmResult = true;
        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Contains("La grille est changée.", _dialogService.ShownInfos);
        Assert.Null(vm.SelectedGrid);
        Assert.False(vm.HasPreview);
        Assert.Equal(1, _service.GetGridsCalls);
    }

    [Theory]
    [InlineData("PRICES_CHANGED")]
    [InlineData("SHARED_GRID")]
    public async Task Apply_ApiErrors_PricesChangedOrSharedGrid_ShowsInfoAndRecomputesPreview(string errorCode)
    {
        var vm = CreateViewModel();
        vm.SelectGridCommand.Execute(new RentalGrid { id = 11, name = "Grille" });

        _service.PreviewResult = new RentalGridPreview
        {
            previewHash = "hash1",
            changes = new List<RentalGridChange> { new() { type = "day", before = 1000, after = 1100, percent = 10 } }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);
        Assert.Single(_service.PreviewCalls);

        var apiError = ApiError.FromHttpResponse($$"""{ "error": "{{errorCode}}" }""", HttpStatusCode.Conflict, null);
        _service.ApplyException = new ApiException(apiError, HttpStatusCode.Conflict);

        _dialogService.ConfirmResult = true;
        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Contains("La grille a changé depuis l'aperçu. Vérifiez le nouvel aperçu.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
        // Nouvel aperçu calculé
        Assert.Equal(2, _service.PreviewCalls.Count);
    }

    [Fact]
    public async Task Apply_ApiError_NothingToChange_ShowsInfoWithoutRecompute()
    {
        var vm = CreateViewModel();
        vm.SelectGridCommand.Execute(new RentalGrid { id = 11, name = "Grille" });

        _service.PreviewResult = new RentalGridPreview
        {
            previewHash = "hash1",
            changes = new List<RentalGridChange> { new() { type = "day", before = 1000, after = 1100, percent = 10 } }
        };

        await vm.ComputePreviewCommand.ExecuteAsync(null);

        var apiError = ApiError.FromHttpResponse("""{ "error": "NOTHING_TO_CHANGE" }""", (HttpStatusCode)422, null);
        _service.ApplyException = new ApiException(apiError, (HttpStatusCode)422);

        _dialogService.ConfirmResult = true;
        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Contains("Rien à changer : les tarifs seraient identiques.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
        // Pas de recompute
        Assert.Single(_service.PreviewCalls);
    }

    [Fact]
    public async Task Undo_CannotUndo_DoesNothing()
    {
        var vm = CreateViewModel();
        // Modification expirée (calculée avec une date passée par rapport à DateTime.Today)
        var modification = new RentalGridModification
        {
            id = 50,
            undoUntil = DateTime.Today.AddDays(-2).ToString("yyyy-MM-ddTHH:mm:sszzz")
        };
        Assert.False(modification.CanUndo);

        await vm.UndoCommand.ExecuteAsync(modification);

        Assert.Empty(_dialogService.ConfirmCalls);
        Assert.Empty(_service.UndoCalls);
    }

    [Fact]
    public async Task Undo_ConfirmationRefused_DoesNotSendRequest()
    {
        var vm = CreateViewModel();
        // Modification encore annulable (date future basée sur DateTime.Today)
        var modification = new RentalGridModification
        {
            id = 51,
            undoUntil = DateTime.Today.AddDays(1).ToString("yyyy-MM-ddTHH:mm:sszzz")
        };
        Assert.True(modification.CanUndo);

        _dialogService.ConfirmResult = false;
        await vm.UndoCommand.ExecuteAsync(modification);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Empty(_service.UndoCalls);
    }

    [Fact]
    public async Task Undo_Confirmed_CallsUndoAndReloads()
    {
        var vm = CreateViewModel();
        var modification = new RentalGridModification
        {
            id = 52,
            pack = new RentalGridPack { name = "Tarif canoë" },
            undoUntil = DateTime.Today.AddDays(1).ToString("yyyy-MM-ddTHH:mm:sszzz")
        };

        _dialogService.ConfirmResult = true;
        await vm.UndoCommand.ExecuteAsync(modification);

        var undoCall = Assert.Single(_service.UndoCalls);
        Assert.Equal(52, undoCall);
        Assert.Contains("Les anciens tarifs sont remis.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
        Assert.Equal(1, _service.GetGridsCalls);
    }

    [Theory]
    [InlineData("UNDO_EXPIRED", "Les 24 heures sont passées : ce changement ne peut plus être annulé.")]
    [InlineData("GRID_EDITED_SINCE", "La grille a été modifiée depuis : l'annulation remettrait d'autres tarifs. Changez-la à nouveau si besoin.")]
    [InlineData("ALREADY_UNDONE", "Ce changement est déjà annulé.")]
    public async Task Undo_ApiErrors_ShowsSpecificInfoMessageWithoutError(string errorCode, string expectedMessage)
    {
        var vm = CreateViewModel();
        var modification = new RentalGridModification
        {
            id = 53,
            undoUntil = DateTime.Today.AddDays(1).ToString("yyyy-MM-ddTHH:mm:sszzz")
        };

        var apiError = ApiError.FromHttpResponse($$"""{ "error": "{{errorCode}}" }""", HttpStatusCode.Conflict, null);
        _service.UndoException = new ApiException(apiError, HttpStatusCode.Conflict);

        _dialogService.ConfirmResult = true;
        await vm.UndoCommand.ExecuteAsync(modification);

        Assert.Contains(expectedMessage, _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
        Assert.Equal(1, _service.GetGridsCalls);
    }
}

