using System.Globalization;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.CollectionCloseUseCase;
using Easymakemoney.UseCase.DashBoardCollectionUseCase;
using Easymakemoney.UseCase.ListCollectionUsecase;
using Easymakemoney.UseCase.PeriodResultUseCase;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Dashboard;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public sealed class DashBoardCollectionViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public DashBoardCollectionViewModelTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private static ListCollection Period(int id = 8, bool isClosed = false, string? reopenedAt = null) => new()
    {
        id = id,
        nomCollection = "Novembre 2026",
        photoCollection = "period.png",
        startDateCollection = "2026-11-01T00:00:00Z",
        endDateCollection = "2026-11-30T23:59:59Z",
        kind = "period",
        IsClosed = isClosed,
        reopenedAt = reopenedAt
    };

    private static (
        DashBoardCollectionViewModel ViewModel,
        FakeDashBoardCollectionService DashService,
        FakeCollectionCloseService CloseService,
        FakePeriodResultService ResultService,
        FakePeriodService PeriodService,
        FakeDialogService Dialog,
        List<string> Routes) Create()
    {
        var dashService = new FakeDashBoardCollectionService();
        var closeService = new FakeCollectionCloseService();
        var resultService = new FakePeriodResultService();
        var periodService = new FakePeriodService();
        var collectionService = new FakeListCollectionService();
        var dialog = new FakeDialogService();
        var routes = new List<string>();

        var vm = new DashBoardCollectionViewModel(
            new GetDashBoardCollectionUseCase(dashService),
            new GetListCollectionsUseCase(collectionService),
            null!,
            null!,
            null!,
            new GetCollectionCloseUseCase(closeService),
            new GetPeriodResultUseCase(resultService),
            new PeriodUseCase(periodService),
            dialog)
        {
            NavigateAsync = route =>
            {
                routes.Add(route);
                return Task.CompletedTask;
            }
        };

        return (vm, dashService, closeService, resultService, periodService, dialog, routes);
    }

    [Fact]
    public async Task SelectCurrentPeriod_Null_StaysEmpty_WithoutDashBoardOrResultCalls()
    {
        var (vm, dashService, _, resultService, periodService, dialog, _) = Create();
        periodService.CurrentPeriodResult = null;

        await vm.SelectCurrentPeriodIfNoneAsync();

        Assert.Equal(0, vm.CollectionId);
        Assert.Null(vm.DashBoardCollection);
        Assert.Null(vm.PeriodResult);
        Assert.False(vm.IsClosedVisible);
        Assert.False(vm.HasPeriodResult);
        Assert.Empty(dashService.RequestedIds);
        Assert.Empty(resultService.RequestedPeriodIds);
        Assert.Empty(dialog.ShownErrors);
    }

    [Fact]
    public async Task SelectCollection_OpenPeriod_LoadsDashBoardAndPeriodResult_SetsPermissionsAndRoutes()
    {
        var (vm, dashService, _, resultService, _, _, routes) = Create();
        resultService.ResultToReturn = new PeriodResult
        {
            sales = new PeriodSales { revenue = 35_000 },
            result = new PeriodTotals { gross = 20_000, net = 12_000 }
        };

        await vm.SelectCollectionAsync(Period(id: 8, isClosed: false));

        Assert.Equal(8, vm.CollectionId);
        Assert.Equal(new[] { 8 }, dashService.RequestedIds);
        Assert.Equal(new[] { 8 }, resultService.RequestedPeriodIds);
        Assert.NotNull(vm.DashBoardCollection);
        Assert.True(vm.HasPeriodResult);
        Assert.True(vm.IsClosedVisible);
        Assert.True(vm.CanEditBudget);
        Assert.False(vm.CanReopen);
        Assert.False(vm.CanClose);

        await vm.OpenBudgetLinesCommand.ExecuteAsync(null);
        await vm.OpenSimulationCommand.ExecuteAsync(null);

        Assert.Equal(["PeriodBudgetPage?PeriodId=8&ReadOnly=0", "PeriodSimulationPage?PeriodId=8"], routes);
    }

    [Fact]
    public async Task SelectCollection_ClosedPeriod_SetsReopenPermission_BlocksSimulation_AndShowsLoadError()
    {
        var (vm, _, _, resultService, _, dialog, routes) = Create();
        resultService.ExceptionToThrow = new ApiException(
            ApiError.FromHttpResponse("""{"code":"SERVER_ERROR","message":"Résultat indisponible."}""", HttpStatusCode.InternalServerError, null),
            HttpStatusCode.InternalServerError);

        await vm.SelectCollectionAsync(Period(id: 8, isClosed: true));

        Assert.True(vm.CanReopen);
        Assert.False(vm.CanEditBudget);
        Assert.False(vm.HasPeriodResult);
        var error = Assert.IsType<ApiException>(Assert.Single(dialog.ShownErrors));
        Assert.Equal("Résultat indisponible.", error.Message);

        await vm.OpenBudgetLinesCommand.ExecuteAsync(null);
        await vm.OpenSimulationCommand.ExecuteAsync(null);

        Assert.Equal(["PeriodBudgetPage?PeriodId=8&ReadOnly=1"], routes);
    }

    [Fact]
    public async Task ReopenCollection_Declined_DoesNotCallService_And_Confirmed_ReopensAndReloads()
    {
        var (vm, dashService, closeService, resultService, _, dialog, _) = Create();
        await vm.SelectCollectionAsync(Period(id: 8, isClosed: true));
        dashService.RequestedIds.Clear();
        resultService.RequestedPeriodIds.Clear();

        dialog.ConfirmResult = false;
        await vm.ReopenCollectionCommand.ExecuteAsync(null);
        Assert.Empty(closeService.ReopenCalls);
        Assert.True(vm.IsClosed);

        dialog.ConfirmResult = true;
        await vm.ReopenCollectionCommand.ExecuteAsync(null);

        Assert.Equal(new[] { 8 }, closeService.ReopenCalls);
        Assert.False(vm.IsClosed);
        Assert.True(vm.IsReopened);
        Assert.True(vm.CanClose);
        Assert.Equal(new[] { 8 }, dashService.RequestedIds);
        Assert.Equal(new[] { 8 }, resultService.RequestedPeriodIds);
    }

    [Fact]
    public async Task ReopenCollection_ServerRefusal_ShowsError_AndKeepsClosedState()
    {
        var (vm, _, closeService, _, _, dialog, _) = Create();
        await vm.SelectCollectionAsync(Period(id: 8, isClosed: true));
        dialog.ConfirmResult = true;
        closeService.ReopenException = new ApiException(
            ApiError.FromHttpResponse("""{"code":"NOT_CLOSED"}""", HttpStatusCode.Conflict, null),
            HttpStatusCode.Conflict);

        await vm.ReopenCollectionCommand.ExecuteAsync(null);

        var error = Assert.IsType<ApiException>(Assert.Single(dialog.ShownErrors));
        Assert.Equal("NOT_CLOSED", error.ApiError?.Code);
        Assert.Equal("Cette période est déjà ouverte.", error.Message);
        Assert.True(vm.IsClosed);
        Assert.False(vm.IsReopened);
    }

    [Fact]
    public async Task CloseCollection_ConfirmationAndServerRefusal_HandlesCancelSuccessAndAlreadyClosed()
    {
        var (vm, dashService, closeService, resultService, _, dialog, _) = Create();
        await vm.SelectCollectionAsync(Period(id: 8, isClosed: false, reopenedAt: "2026-11-10T10:00:00Z"));
        dashService.RequestedIds.Clear();
        resultService.RequestedPeriodIds.Clear();

        // Refus de confirmation : aucune requête envoyée
        dialog.ConfirmResult = false;
        await vm.CloseCollectionCommand.ExecuteAsync(null);
        Assert.Empty(closeService.CloseCalls);
        Assert.False(vm.IsClosed);

        // Confirmation acceptée : clôture, message et rechargement du résultat
        // Note : CloseCollection appelle GetDashBoardCollectionAsync avant de libérer IsBusy (ligne 269), donc dashService n'est pas rappelé ici.
        dialog.ConfirmResult = true;
        await vm.CloseCollectionCommand.ExecuteAsync(null);
        Assert.Equal(new[] { 8 }, closeService.CloseCalls);
        Assert.True(vm.IsClosed);
        Assert.Contains("Période clôturée", dialog.ShownInfos);
        Assert.Empty(dashService.RequestedIds);
        Assert.Equal(new[] { 8 }, resultService.RequestedPeriodIds);

        // Refus serveur ALREADY_CLOSED : garde IsClosed inchangé (remis à false avant l'essai) et montre le message
        vm.IsClosed = false;
        closeService.CloseException = new ApiException(
            ApiError.FromHttpResponse("""{"code":"ALREADY_CLOSED"}""", HttpStatusCode.Conflict, null),
            HttpStatusCode.Conflict);
        await vm.CloseCollectionCommand.ExecuteAsync(null);

        var error = Assert.IsType<ApiException>(Assert.Single(dialog.ShownErrors));
        Assert.Equal("ALREADY_CLOSED", error.ApiError?.Code);
        Assert.Equal("Cet élément est déjà clôturé.", error.Message);
        Assert.False(vm.IsClosed);
    }

    [Fact]
    public async Task EditBudget_ValidAmountAndInvalidAmount_UpdatesBudgetOrShowsError()
    {
        var (vm, dashService, _, _, periodService, dialog, _) = Create();
        await vm.SelectCollectionAsync(Period(id: 8, isClosed: false));
        dashService.RequestedIds.Clear();

        dialog.PromptResult = "-5";
        await vm.EditBudgetCommand.ExecuteAsync(null);
        Assert.Empty(periodService.UpdateBudgetCalls);
        Assert.Contains("Montant invalide.", dialog.ShownInfos);

        dialog.PromptResult = "1 500,50";
        await vm.EditBudgetCommand.ExecuteAsync(null);
        Assert.Single(periodService.UpdateBudgetCalls);
        Assert.Equal((8, 150_050L), periodService.UpdateBudgetCalls[0]);
        Assert.Equal(new[] { 8 }, dashService.RequestedIds);
    }
}
