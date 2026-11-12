using System.Net;
using CommunityToolkit.Mvvm.Input;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.ListColorsUseCase;
using Easymakemoney.UseCase.ListProductVariantUseCase;
using Easymakemoney.UseCase.ListSizesUseCase;
using Easymakemoney.ViewModels.FormModels;
using Easymakemoney.ViewModels.Lists;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

public class ListNewProductsVariantViewModelTests
{
    private static ColorsProduct SampleColor(int id = 1, string name = "Noir") => new()
    {
        id = id,
        name = name,
        codeHexa = "#000000"
    };

    private static SizeProduct SampleSize(int id = 2, string name = "M") => new()
    {
        id = id,
        name = name
    };

    private static ProductVariant SampleVariant(int id = 10, int stock = 5) => new()
    {
        id = id,
        color = SampleColor(),
        size = SampleSize(),
        stockQuantity = stock
    };

    private static (
        ListNewProductsVariantViewModel ViewModel,
        FakeListProductVariantService Service,
        FakeDialogService Dialog,
        ProductVariantFormModel FormModel,
        SaveProductvariantUseCase SaveUseCase) Create()
    {
        var service = new FakeListProductVariantService();
        var colorsService = new FakeListColorsService();
        colorsService.Colors.Add(SampleColor());
        var sizesService = new FakeListSizesService();
        sizesService.Sizes.Add(SampleSize());

        var formModel = new ProductVariantFormModel(
            new GetListColorsUseCase(colorsService),
            new GetListSizesUseCase(sizesService));
        var saveUseCase = new SaveProductvariantUseCase(new CreateProductsVariantUseCase(service));
        var dialog = new FakeDialogService();

        var vm = new ListNewProductsVariantViewModel(
            new GetListProductsVariantUseCase(service),
            new DeleteProductsVariantUseCase(service),
            saveUseCase,
            formModel,
            dialog)
        {
            ProductId = 42
        };

        return (vm, service, dialog, formModel, saveUseCase);
    }

    [Fact]
    public async Task GetListProductVariant_LoadsVariants_AndHandlesServerError()
    {
        var (vm, service, dialog, _, _) = Create();
        service.Variants.Add(SampleVariant(10, stock: 7));

        await vm.GetlistProductVariantAsync();

        Assert.Equal(new[] { 42 }, service.GetCalls);
        var loaded = Assert.Single(vm.ListProductVariants);
        Assert.Equal(10, loaded.id);
        Assert.Equal(7, loaded.stockQuantity);
        Assert.False(vm.IsBusy);

        service.GetException = new ApiException(
            ApiError.FromHttpResponse("""{"message":"Erreur chargement variantes."}""", HttpStatusCode.InternalServerError, null),
            HttpStatusCode.InternalServerError);

        await vm.GetlistProductVariantAsync();

        Assert.Empty(vm.ListProductVariants);
        var error = Assert.IsType<ApiException>(Assert.Single(dialog.ShownErrors));
        Assert.Equal("Erreur chargement variantes.", error.Message);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task CreateVariant_SavesViaFormModelAndUseCase_AndReloadsList()
    {
        var (vm, service, _, formModel, saveUseCase) = Create();
        formModel.SelectedColor = Assert.Single(formModel.Colors);
        formModel.SelectedSize = Assert.Single(formModel.Sizes);
        formModel.StockQuantity = 14;

        var saved = await saveUseCase.ExecuteAsync(formModel, vm.ProductId);
        await vm.GetlistProductVariantAsync();

        Assert.True(saved);
        Assert.Single(service.PostCalls);
        Assert.Equal(42, service.PostCalls[0].ProductId);
        Assert.Equal(14, service.PostCalls[0].Variant.stockQuantity);
        Assert.Equal("Noir", service.PostCalls[0].Variant.color.name);
        Assert.Equal("M", service.PostCalls[0].Variant.size.name);
        var reloaded = Assert.Single(vm.ListProductVariants);
        Assert.Equal(14, reloaded.stockQuantity);
    }

    [Fact]
    public async Task DeleteVariant_Declined_DoesNotCallService_AndKeepsItem()
    {
        var (vm, service, dialog, _, _) = Create();
        var variant = SampleVariant(10);
        service.Variants.Add(variant);
        await vm.GetlistProductVariantAsync();
        dialog.ConfirmResult = false;

        await ((IAsyncRelayCommand<ProductVariant>)vm.DeleteProductVariantCommand).ExecuteAsync(variant);

        Assert.Empty(service.DeleteCalls);
        Assert.Single(vm.ListProductVariants);
    }

    [Fact]
    public async Task DeleteVariant_Confirmed_RemovesItem_AndShowsSuccessInfo()
    {
        var (vm, service, dialog, _, _) = Create();
        var variant = SampleVariant(10);
        service.Variants.Add(variant);
        await vm.GetlistProductVariantAsync();
        dialog.ConfirmResult = true;

        await ((IAsyncRelayCommand<ProductVariant>)vm.DeleteProductVariantCommand).ExecuteAsync(variant);

        Assert.Equal(new[] { 10 }, service.DeleteCalls);
        Assert.Empty(vm.ListProductVariants);
        Assert.Contains("Variante supprimée avec succès.", dialog.ShownInfos);
    }

    [Fact]
    public async Task DeleteVariant_Error_ProductInUse_KeepsItem_AndShowsApiErrorMessage()
    {
        var (vm, service, dialog, _, _) = Create();
        var variant = SampleVariant(10);
        service.Variants.Add(variant);
        await vm.GetlistProductVariantAsync();
        dialog.ConfirmResult = true;
        service.DeleteException = new ApiException(
            ApiError.FromHttpResponse("""{"code":"PRODUCT_IN_USE"}""", HttpStatusCode.Conflict, null),
            HttpStatusCode.Conflict);

        await ((IAsyncRelayCommand<ProductVariant>)vm.DeleteProductVariantCommand).ExecuteAsync(variant);

        Assert.Single(vm.ListProductVariants);
        var error = Assert.IsType<ApiException>(Assert.Single(dialog.ShownErrors));
        Assert.Equal("PRODUCT_IN_USE", error.ApiError?.Code);
        Assert.Equal(
            "Cet article a des ventes, des achats ou des mouvements de stock : retirez-le de la vente au lieu de le supprimer.",
            error.Message);
    }
}
