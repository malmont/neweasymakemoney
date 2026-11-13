using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.Input;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Models.DashboardCommand;
using Easymakemoney.Models.DashbordCollection;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.DashboardCommandUseCase;
using Easymakemoney.UseCase.LisCommandeUseCase;
using Easymakemoney.UseCase.ListFournisseurUseCase;
using Easymakemoney.UseCase.ListProductUseCase;
using Easymakemoney.UseCase.ProductAttachmentUseCase;
using Easymakemoney.ViewModels.Dashboard;
using Easymakemoney.ViewModels.FormModels;
using Easymakemoney.ViewModels.Lists;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

public sealed class DashBoardCommandAndProductViewModelTests
{
    private static DashboardCommand SampleDashboardCommand(double itemCost = 50000, double shipping = 4500) => new()
    {
        averageMultiplier = 2.5,
        totalItemCost = itemCost,
        totalFraisDePort = shipping,
        budgetGeneral = new BudgetGeneral { generalBudget = 100000, usedBudget = 54500, remainingBudget = 45500 },
        statistics = new Statistics { orderCount = 1, itemCount = 12, modelCount = 4 },
        valeurStock = new ValeurStock { stockValue = 125000, marge = 70500 },
        tauxMarge = new TauxMarge { tauxMarge = 56.4, tauxMarque = 129.0 },
        transporteur = new Transporteur { id = 3, name = "Chronopost" }
    };

    private static (
        DashBoardCommandViewModel ViewModel,
        FakeDashboardCommandServices DashService,
        FakeListCommandService CommandListService,
        FakeDialogService Dialog,
        List<string> Routes) CreateCommandVm()
    {
        var dashService = new FakeDashboardCommandServices();
        var commandListService = new FakeListCommandService();
        var fournisseurService = new FakeListFournisseursServices();
        var tripService = new FakeTripService();
        var dialog = new FakeDialogService();
        var form = new CommandFormModel(new GetListFournisseursUseCase(fournisseurService));
        var saveCommand = new SaveCommandUseCase(new CreateCommandUseCase(commandListService));
        var getListCommand = new GetListCommandUseCase(commandListService);
        var listVm = new ListNewCommandViewModel(
            getListCommand,
            saveCommand,
            new DeleteCommandUseCase(commandListService),
            form,
            dialog,
            tripService);

        var routes = new List<string>();
        var vm = new DashBoardCommandViewModel(
            new GetDashboardCommandUseCase(dashService),
            listVm,
            form,
            saveCommand,
            getListCommand,
            dialog)
        {
            NavigateAsync = route =>
            {
                routes.Add(route);
                return Task.CompletedTask;
            }
        };

        return (vm, dashService, commandListService, dialog, routes);
    }

    private static (
        DashBoardProductViewModel ViewModel,
        ListNewProductViewModel ProductListVm,
        FakeListProductsService ProductService,
        FakeDialogService Dialog) CreateProductVm()
    {
        var productService = new FakeListProductsService();
        var categoryService = new FakeListCategoryServices();
        var styleService = new FakeListStyleService();
        var attachmentService = new FakeProductAttachmentService();
        var dialog = new FakeDialogService();
        var form = new ProductFormModel(
            new Easymakemoney.UseCase.ListCategoryUseCase.GetListCategoryUseCase(categoryService),
            new EEasymakemoney.UseCase.ListStyleUsesCase.GetListStyleUsesCases(styleService));
        var getList = new GetListProductsUseCase(productService);
        var saveProduct = new SaveProductUseCase(new CreateProductsUseCase(productService));
        var listVm = new ListNewProductViewModel(
            getList,
            form,
            saveProduct,
            new DeleteProductsUseCase(productService),
            new DetachProductUseCase(attachmentService),
            dialog);

        var vm = new DashBoardProductViewModel(listVm, form, saveProduct, getList, dialog);
        return (vm, listVm, productService, dialog);
    }

    [Fact]
    public async Task DashBoardCommand_GetDashboardCommand_Success_PopulatesAndRespectsIsBusy()
    {
        var (vm, dashService, _, dialog, _) = CreateCommandVm();
        var tcs = new TaskCompletionSource<DashboardCommand>();
        dashService.PendingTask = tcs;

        var firstLoad = vm.GetDashboardCommandAsync(14);
        Assert.True(vm.IsBusy);

        await vm.GetDashboardCommandAsync(99);
        Assert.Equal(new[] { 14 }, dashService.RequestedIds);

        var expected = SampleDashboardCommand();
        tcs.SetResult(expected);
        await firstLoad;

        Assert.Same(expected, vm.DashBoardCommand);
        Assert.False(vm.IsBusy);
        Assert.Empty(dialog.ShownErrors);
    }

    [Fact]
    public async Task DashBoardCommand_GetDashboardCommand_Error_ShowsApiError_AndResetsIsBusy()
    {
        var (vm, dashService, _, dialog, _) = CreateCommandVm();
        dashService.ExceptionToThrow = new ApiException(
            new ApiError { Code = "NOT_FOUND", Message = "Commande introuvable." },
            HttpStatusCode.NotFound);

        await vm.GetDashboardCommandAsync(77);

        Assert.Null(vm.DashBoardCommand);
        Assert.False(vm.IsBusy);
        var err = Assert.IsType<ApiException>(Assert.Single(dialog.ShownErrors));
        Assert.Equal("Commande introuvable.", err.ApiError.Message);
    }

    [Fact]
    public async Task DashBoardCommand_WithoutPeriod_CreateAndList_ShowRequiredPeriodInfo()
    {
        var (vm, dashService, commandService, dialog, _) = CreateCommandVm();
        vm.CollectionId = 0;

        await vm.ShowBottomSheetCreateCommand();
        await vm.ShowBottomSheetPopupListCommandView();

        Assert.Equal(2, dialog.ShownInfos.Count);
        Assert.All(dialog.ShownInfos, msg => Assert.Equal("Vous devez choisir obligatoirement une période", msg));
        Assert.Empty(commandService.GetCalls);
        Assert.Empty(dashService.RequestedIds);
    }

    [Fact]
    public async Task DashBoardCommand_LoadSelectAndNavigate_HandlesEmptyErrorSelectionAndRoute()
    {
        var (vm, dashService, commandService, dialog, routes) = CreateCommandVm();
        vm.CollectionId = 8;

        var emptyResult = await vm.LoadCommandsAsync();
        Assert.Empty(emptyResult);
        Assert.Equal(new[] { 8 }, commandService.GetCalls);
        Assert.Empty(dialog.ShownErrors);

        commandService.GetException = new ApiException(
            new ApiError { Code = "SERVER_ERROR", Message = "Erreur de chargement des commandes." },
            HttpStatusCode.InternalServerError);
        var errorResult = await vm.LoadCommandsAsync();
        Assert.Empty(errorResult);
        Assert.Single(dialog.ShownErrors);

        await ((IAsyncRelayCommand)vm.ItemTappedCommand).ExecuteAsync(null);
        Assert.Empty(routes);

        dashService.Result = SampleDashboardCommand();
        await vm.SelectCommandAsync(new ListCommand
        {
            id = 14,
            name = "Commande Milan",
            photo = "milan.png",
            date = "2026-11-12"
        });

        Assert.Equal(14, vm.CommandId);
        Assert.Equal("Commande Milan", vm.CommandName);
        Assert.Equal("milan.png", vm.CommandImage);
        Assert.NotNull(vm.DashBoardCommand);
        Assert.Equal(new[] { 14 }, dashService.RequestedIds);

        await ((IAsyncRelayCommand)vm.ItemTappedCommand).ExecuteAsync(null);
        Assert.Equal(new[] { "DashBoardProductPage?CommandId=14" }, routes);
    }

    [Fact]
    public async Task DashBoardProduct_WithoutCommand_CreateAndList_ShowRequiredCommandInfo()
    {
        var (vm, _, productService, dialog) = CreateProductVm();
        vm.CommandId = 0;

        await vm.ShowBottomSheetCreateProduct();
        await vm.ShowBottomSheetPopupListProductView();

        Assert.Equal(2, dialog.ShownInfos.Count);
        Assert.All(dialog.ShownInfos, msg => Assert.Equal("Vous devez choisir obligatoirement une commande", msg));
        Assert.Empty(productService.GetCalls);
    }

    [Fact]
    public void DashBoardProduct_SelectProduct_UpdatesFieldsAndDisplayedQuantity()
    {
        var (vm, _, _, _) = CreateProductVm();

        var frozenProduct = new ListProduct
        {
            id = 30,
            name = "Manteau laine",
            image = "manteau.png",
            freezeQuantity = 7,
            quantity = 12
        };
        vm.SelectProduct(frozenProduct);

        Assert.Same(frozenProduct, vm.DashBoardProduct);
        Assert.Equal("Manteau laine", vm.ProductName);
        Assert.Equal("manteau.png", vm.ProductImage);
        Assert.Equal(7, vm.DisplayedQuantity);

        vm.SelectProduct(new ListProduct
        {
            id = 31,
            name = "Écharpe",
            image = "echarpe.png",
            freezeQuantity = null,
            quantity = 12
        });
        Assert.Equal(12, vm.DisplayedQuantity);

        vm.SelectProduct(new ListProduct
        {
            id = 32,
            name = "Bonnet",
            image = "bonnet.png",
            freezeQuantity = null,
            quantity = null
        });
        Assert.Equal(0, vm.DisplayedQuantity);
    }

    [Fact]
    public async Task DashBoardProduct_LoadProducts_EmptyAndServerError_ReturnsEmptyAndShowsError()
    {
        var (vm, _, productService, dialog) = CreateProductVm();
        vm.CommandId = 14;

        var emptyResult = await vm.LoadProductsAsync();
        Assert.Empty(emptyResult);
        Assert.Equal(new[] { 14 }, productService.GetCalls);
        Assert.Empty(dialog.ShownErrors);

        productService.GetException = new ApiException(
            new ApiError { Code = "UPSTREAM_ERROR", Message = "Impossible de charger les articles." },
            HttpStatusCode.BadGateway);
        var errorResult = await vm.LoadProductsAsync();

        Assert.Empty(errorResult);
        var err = Assert.IsType<ApiException>(Assert.Single(dialog.ShownErrors));
        Assert.Equal("Impossible de charger les articles.", err.ApiError.Message);
    }
}
