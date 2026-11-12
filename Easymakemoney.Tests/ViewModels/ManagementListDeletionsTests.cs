using System.Net;
using CommunityToolkit.Mvvm.Input;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.LisCommandeUseCase;
using Easymakemoney.UseCase.ListCollectionUsecase;
using Easymakemoney.UseCase.ListFournisseurUseCase;
using Easymakemoney.UseCase.ListFraisDePortUseCase;
using Easymakemoney.UseCase.ListNoteDeFraisUseCase;
using Easymakemoney.UseCase.ListProductUseCase;
using Easymakemoney.UseCase.PeriodResultUseCase;
using Easymakemoney.UseCase.ProductAttachmentUseCase;
using Easymakemoney.ViewModels.Lists;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

public class ManagementListDeletionsTests
{
    private static ListCommand SampleCommand(int id, string name = "Commande") => new()
    {
        id = id,
        name = name,
        photo = "photo.png",
        date = "2026-11-10"
    };

    private static FraisDePort SampleFrais(int id = 1, long price = 1500) => new()
    {
        id = id,
        name = "Transporteur",
        image = "transport.png",
        price = price
    };

    [Fact]
    public async Task Command_Delete_Declined_DoesNotCallService()
    {
        var fakeService = new FakeListCommandService();
        var fakeDialog = new FakeDialogService { ConfirmResult = false };
        var vm = new ListNewCommandViewModel(
            new GetListCommandUseCase(fakeService),
            null!,
            new DeleteCommandUseCase(fakeService),
            null!,
            fakeDialog,
            new FakeTripService());

        var command = SampleCommand(10);
        await ((IAsyncRelayCommand<ListCommand>)vm.DeleteCommandCommand).ExecuteAsync(command);

        Assert.Empty(fakeService.DeleteCalls);
    }

    [Fact]
    public async Task Command_Delete_Confirmed_CallsDelete_AndRefreshes()
    {
        var fakeService = new FakeListCommandService();
        fakeService.Commands.Add(SampleCommand(10));
        var fakeDialog = new FakeDialogService { ConfirmResult = true };
        var vm = new ListNewCommandViewModel(
            new GetListCommandUseCase(fakeService),
            null!,
            new DeleteCommandUseCase(fakeService),
            null!,
            fakeDialog,
            new FakeTripService())
        {
            CollectionId = 5
        };

        var command = SampleCommand(10);
        await ((IAsyncRelayCommand<ListCommand>)vm.DeleteCommandCommand).ExecuteAsync(command);

        Assert.Single(fakeService.DeleteCalls);
        Assert.Equal(10, fakeService.DeleteCalls[0]);
        Assert.Contains(5, fakeService.GetCalls);
        Assert.Contains(fakeDialog.ShownInfos, m => m.Contains("supprimée avec succès"));
    }

    [Fact]
    public async Task Command_Delete_Error_PeriodClosed_DisplaysApiErrorMessage()
    {
        var fakeService = new FakeListCommandService
        {
            DeleteException = new ApiException(
                ApiError.FromHttpResponse("""{"error": "PERIOD_CLOSED"}""", HttpStatusCode.Conflict, null),
                HttpStatusCode.Conflict)
        };
        var fakeDialog = new FakeDialogService { ConfirmResult = true };
        var vm = new ListNewCommandViewModel(
            new GetListCommandUseCase(fakeService),
            null!,
            new DeleteCommandUseCase(fakeService),
            null!,
            fakeDialog,
            new FakeTripService());

        var command = SampleCommand(10);
        await ((IAsyncRelayCommand<ListCommand>)vm.DeleteCommandCommand).ExecuteAsync(command);

        Assert.Single(fakeDialog.ShownErrors);
        Assert.Equal("Cette période est clôturée : plus rien ne peut y être ajouté.", fakeDialog.ShownErrors[0].Message);
    }

    [Fact]
    public async Task Fournisseur_Delete_Declined_DoesNotCallService()
    {
        var fakeService = new FakeListFournisseursServices();
        var fakeDialog = new FakeDialogService { ConfirmResult = false };
        var vm = new ListNewFournisseurViewModel(
            new GetListFournisseursUseCase(fakeService),
            new DeleteFournisseurUseCase(fakeService),
            null!,
            null!,
            fakeDialog);

        var f = new ListFournisseur { id = 20, name = "Fournisseur A" };
        await ((IAsyncRelayCommand<ListFournisseur>)vm.DeleteFournisseurCommand).ExecuteAsync(f);

        Assert.Empty(fakeService.DeleteCalls);
    }

    [Fact]
    public async Task Fournisseur_Delete_Confirmed_CallsDelete_AndRemovesFromList()
    {
        var fakeService = new FakeListFournisseursServices();
        var fakeDialog = new FakeDialogService { ConfirmResult = true };
        var vm = new ListNewFournisseurViewModel(
            new GetListFournisseursUseCase(fakeService),
            new DeleteFournisseurUseCase(fakeService),
            null!,
            null!,
            fakeDialog);

        var f = new ListFournisseur { id = 20, name = "Fournisseur A" };
        vm.ListFournisseurs.Add(f);

        await ((IAsyncRelayCommand<ListFournisseur>)vm.DeleteFournisseurCommand).ExecuteAsync(f);

        Assert.Single(fakeService.DeleteCalls);
        Assert.Equal(20, fakeService.DeleteCalls[0]);
        Assert.DoesNotContain(f, vm.ListFournisseurs);
        Assert.Contains(fakeDialog.ShownInfos, m => m.Contains("supprimé avec succès"));
    }

    [Fact]
    public async Task Fournisseur_Delete_Error_FournisseurInUse_DisplaysApiErrorMessage()
    {
        var fakeService = new FakeListFournisseursServices
        {
            DeleteException = new ApiException(
                ApiError.FromHttpResponse("""{"error": "FOURNISSEUR_IN_USE", "message": "Ce fournisseur a des commandes."}""", HttpStatusCode.Conflict, null),
                HttpStatusCode.Conflict)
        };
        var fakeDialog = new FakeDialogService { ConfirmResult = true };
        var vm = new ListNewFournisseurViewModel(
            new GetListFournisseursUseCase(fakeService),
            new DeleteFournisseurUseCase(fakeService),
            null!,
            null!,
            fakeDialog);

        var f = new ListFournisseur { id = 20, name = "Fournisseur A" };
        vm.ListFournisseurs.Add(f);

        await ((IAsyncRelayCommand<ListFournisseur>)vm.DeleteFournisseurCommand).ExecuteAsync(f);

        Assert.Single(fakeDialog.ShownErrors);
        Assert.Contains("Ce fournisseur a des commandes.", fakeDialog.ShownErrors[0].Message);
        Assert.Contains(f, vm.ListFournisseurs);
    }

    [Fact]
    public async Task NoteDeFrais_Delete_Declined_DoesNotCallService()
    {
        var fakeNoteService = new FakeListNoteDeFraisServices();
        var fakeDialog = new FakeDialogService { ConfirmResult = false };
        var periodUseCase = new PeriodUseCase(new FakePeriodService());

        var vm = new ListNewNoteDeFraisViewModel(
            null!,
            new GetListNoteDeFraisUseCase(fakeNoteService),
            new DeleteNoteDeFraisUseCase(fakeNoteService),
            null!,
            null!,
            null!,
            null!,
            null!,
            periodUseCase,
            fakeDialog,
            new FakeTripService());

        var note = new NoteDeFrais { id = 30, name = "Taxi" };
        await ((IAsyncRelayCommand<NoteDeFrais>)vm.DeleteNoteDefraisCommand).ExecuteAsync(note);

        Assert.Empty(fakeNoteService.DeleteCalls);
    }

    [Fact]
    public async Task NoteDeFrais_Delete_Confirmed_CallsDelete_AndRemovesFromList()
    {
        var fakeNoteService = new FakeListNoteDeFraisServices();
        var fakeDialog = new FakeDialogService { ConfirmResult = true };
        var periodUseCase = new PeriodUseCase(new FakePeriodService());

        var vm = new ListNewNoteDeFraisViewModel(
            null!,
            new GetListNoteDeFraisUseCase(fakeNoteService),
            new DeleteNoteDeFraisUseCase(fakeNoteService),
            null!,
            null!,
            null!,
            null!,
            null!,
            periodUseCase,
            fakeDialog,
            new FakeTripService());

        var note = new NoteDeFrais { id = 30, name = "Taxi" };
        vm.ListNoteDeFrais.Add(note);

        await ((IAsyncRelayCommand<NoteDeFrais>)vm.DeleteNoteDefraisCommand).ExecuteAsync(note);

        Assert.Single(fakeNoteService.DeleteCalls);
        Assert.Equal(30, fakeNoteService.DeleteCalls[0]);
        Assert.DoesNotContain(note, vm.ListNoteDeFrais);
        Assert.Contains(fakeDialog.ShownInfos, m => m.Contains("supprimée avec succès"));
    }

    [Fact]
    public async Task FraisDePort_Delete_Confirmed_CallsDelete_AndRefreshes()
    {
        var fakeFraisService = new FakeListFraisDePortService();
        var fakeCommandService = new FakeListCommandService();
        var fakeDialog = new FakeDialogService { ConfirmResult = true };
        var periodUseCase = new PeriodUseCase(new FakePeriodService());

        var vm = new ListNewFraisDePortViewModel(
            null!,
            new GetListCommandUseCase(fakeCommandService),
            new GetListFraisDePortUseCase(fakeFraisService),
            new DeleteFraisDePortUseCase(fakeFraisService),
            null!,
            periodUseCase,
            null!,
            fakeDialog)
        {
            CollectionId = 2
        };

        var row = new FraisDePortCommandeRow
        {
            Commande = SampleCommand(40),
            Frais = SampleFrais(1, 1500)
        };

        await ((IAsyncRelayCommand<FraisDePortCommandeRow>)vm.DeleteFraisDePortCommand).ExecuteAsync(row);

        Assert.Single(fakeFraisService.DeleteCalls);
        Assert.Equal(40, fakeFraisService.DeleteCalls[0]);
        Assert.Contains(fakeDialog.ShownInfos, m => m.Contains("supprimé avec succès"));
    }

    [Fact]
    public async Task Product_DetachProduct_Confirmed_CallsDetach_AndRemovesFromList()
    {
        var fakeProductService = new FakeListProductsService();
        var fakeAttachmentService = new FakeProductAttachmentService();
        var fakeDialog = new FakeDialogService { ConfirmResult = true };

        var vm = new ListNewProductViewModel(
            new GetListProductsUseCase(fakeProductService),
            null!,
            null!,
            new DeleteProductsUseCase(fakeProductService),
            new DetachProductUseCase(fakeAttachmentService),
            fakeDialog)
        {
            CommandId = 50
        };

        var product = new ListProduct { id = 100, name = "Article A" };
        vm.ListProducts.Add(product);

        await ((IAsyncRelayCommand<ListProduct>)vm.DetachProductCommand).ExecuteAsync(product);

        Assert.Single(fakeAttachmentService.DetachCalls);
        Assert.Equal((50, 100), fakeAttachmentService.DetachCalls[0]);
        Assert.DoesNotContain(product, vm.ListProducts);
    }

    [Fact]
    public async Task Product_DeleteProduct_Error_ProductInUse_DisplaysApiErrorMessage()
    {
        var fakeProductService = new FakeListProductsService
        {
            DeleteException = new ApiException(
                ApiError.FromHttpResponse("""{"error": "PRODUCT_IN_USE"}""", HttpStatusCode.Conflict, null),
                HttpStatusCode.Conflict)
        };
        var fakeAttachmentService = new FakeProductAttachmentService();
        var fakeDialog = new FakeDialogService { ConfirmResult = true };

        var vm = new ListNewProductViewModel(
            new GetListProductsUseCase(fakeProductService),
            null!,
            null!,
            new DeleteProductsUseCase(fakeProductService),
            new DetachProductUseCase(fakeAttachmentService),
            fakeDialog);

        var product = new ListProduct { id = 100, name = "Article A" };
        vm.ListProducts.Add(product);

        await ((IAsyncRelayCommand<ListProduct>)vm.DeleteProductCommand).ExecuteAsync(product);

        Assert.Single(fakeDialog.ShownErrors);
        Assert.Equal("Cet article a des ventes, des achats ou des mouvements de stock : retirez-le de la vente au lieu de le supprimer.", fakeDialog.ShownErrors[0].Message);
        Assert.Contains(product, vm.ListProducts);
    }
}
