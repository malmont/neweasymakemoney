using System.Net;
using CommunityToolkit.Mvvm.Input;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.LisCommandeUseCase;
using Easymakemoney.UseCase.ListCollectionUsecase;
using Easymakemoney.UseCase.ProductAttachmentUseCase;
using Easymakemoney.ViewModels.Lists;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

public class AttachProductsViewModelTests
{
    [Fact]
    public void ToggleProduct_UpdatesSelectionAndCount()
    {
        var fakeAttachment = new FakeProductAttachmentService();
        var fakeDialog = new FakeDialogService();
        var vm = new AttachProductsViewModel(
            new GetAvailableProductsUseCase(fakeAttachment),
            new AttachProductsUseCase(fakeAttachment),
            new GetListCollectionsUseCase(new FakeListCollectionService()),
            new GetListCommandUseCase(new FakeListCommandService()),
            fakeDialog);

        var product = new AvailableProduct { id = 1, name = "Robe" };
        Assert.False(vm.HasSelection);
        Assert.Equal(0, vm.SelectedCount);

        vm.ToggleProductCommand.Execute(product);
        Assert.True(product.IsSelected);
        Assert.True(vm.HasSelection);
        Assert.Equal(1, vm.SelectedCount);
        Assert.Equal("Associer 1 article", vm.ConfirmText);

        vm.CommandId = 42;
        Assert.Equal("Ajouter 1 article", vm.ConfirmText);

        vm.ToggleProductCommand.Execute(product);
        Assert.False(product.IsSelected);
        Assert.False(vm.HasSelection);
        Assert.Equal(0, vm.SelectedCount);
    }

    [Fact]
    public async Task Confirm_WithCommandId_AttachesSelectedProducts()
    {
        var fakeAttachment = new FakeProductAttachmentService();
        var fakeDialog = new FakeDialogService();
        var vm = new AttachProductsViewModel(
            new GetAvailableProductsUseCase(fakeAttachment),
            new AttachProductsUseCase(fakeAttachment),
            new GetListCollectionsUseCase(new FakeListCollectionService()),
            new GetListCommandUseCase(new FakeListCommandService()),
            fakeDialog)
        {
            CommandId = 42
        };

        var p1 = new AvailableProduct { id = 1, name = "P1" };
        var p2 = new AvailableProduct { id = 2, name = "P2" };
        vm.ToggleProductCommand.Execute(p1);
        vm.ToggleProductCommand.Execute(p2);

        await ((IAsyncRelayCommand)vm.ConfirmCommand).ExecuteAsync(null);

        Assert.Single(fakeAttachment.AttachCalls);
        Assert.Equal(42, fakeAttachment.AttachCalls[0].CommandId);
        Assert.Equal(2, fakeAttachment.AttachCalls[0].Products.Count);
        Assert.Contains(fakeDialog.ShownInfos, m => m.Contains("2 articles ajoutés à la commande."));
    }

    [Fact]
    public async Task Confirm_WithoutSelection_DoesNothing()
    {
        var fakeAttachment = new FakeProductAttachmentService();
        var fakeDialog = new FakeDialogService();
        var vm = new AttachProductsViewModel(
            new GetAvailableProductsUseCase(fakeAttachment),
            new AttachProductsUseCase(fakeAttachment),
            new GetListCollectionsUseCase(new FakeListCollectionService()),
            new GetListCommandUseCase(new FakeListCommandService()),
            fakeDialog)
        {
            CommandId = 42
        };

        await ((IAsyncRelayCommand)vm.ConfirmCommand).ExecuteAsync(null);

        Assert.Empty(fakeAttachment.AttachCalls);
    }

    [Fact]
    public async Task Confirm_Error_PeriodClosed_DisplaysApiErrorMessage()
    {
        var fakeAttachment = new FakeProductAttachmentService
        {
            AttachException = new ApiException(
                ApiError.FromHttpResponse("""{"error": "PERIOD_CLOSED"}""", HttpStatusCode.Conflict, null),
                HttpStatusCode.Conflict)
        };
        var fakeDialog = new FakeDialogService();
        var vm = new AttachProductsViewModel(
            new GetAvailableProductsUseCase(fakeAttachment),
            new AttachProductsUseCase(fakeAttachment),
            new GetListCollectionsUseCase(new FakeListCollectionService()),
            new GetListCommandUseCase(new FakeListCommandService()),
            fakeDialog)
        {
            CommandId = 42
        };

        var p1 = new AvailableProduct { id = 1, name = "P1" };
        vm.ToggleProductCommand.Execute(p1);

        await ((IAsyncRelayCommand)vm.ConfirmCommand).ExecuteAsync(null);

        Assert.Single(fakeDialog.ShownErrors);
        Assert.Equal("Cette période est clôturée : plus rien ne peut y être ajouté.", fakeDialog.ShownErrors[0].Message);
    }

    [Fact]
    public async Task NoteDeFrais_ShowBottomSheet_WithoutCollection_ShowsWarning()
    {
        var fakeDialog = new FakeDialogService();

        var vm = new ListNewNoteDeFraisViewModel(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            fakeDialog,
            null!)
        {
            CollectionId = 0
        };

        await ((IAsyncRelayCommand)vm.ShowBottomSheetCommand).ExecuteAsync(null);

        Assert.Contains(fakeDialog.ShownInfos, m => m.Contains("choisir obligatoirement une période"));
    }

    [Fact]
    public async Task Command_ChooseTrip_WhenClosed_DoesNotOpenPicker()
    {
        var fakeTripService = new FakeTripService();
        var fakeDialog = new FakeDialogService();

        var vm = new ListNewCommandViewModel(
            null!,
            null!,
            null!,
            null!,
            fakeDialog,
            fakeTripService)
        {
            IsClosed = true
        };

        Assert.False(vm.CanEdit);
        var command = new ListCommand { id = 10, name = "Commande 10", photo = "photo.png", date = "2026-11-10" };
        await ((IAsyncRelayCommand<ListCommand>)vm.ChooseTripCommand).ExecuteAsync(command);

        Assert.Empty(fakeTripService.GetTripsCalls);
    }
}
