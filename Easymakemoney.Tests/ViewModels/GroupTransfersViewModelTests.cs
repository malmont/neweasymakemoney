using System.Net;
using CommunityToolkit.Mvvm.Input;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.DashboardCommandUseCase;
using Easymakemoney.UseCase.LisCommandeUseCase;
using Easymakemoney.UseCase.ListProductUseCase;
using Easymakemoney.UseCase.ProductAttachmentUseCase;
using Easymakemoney.ViewModels.Dashboard;
using Easymakemoney.ViewModels.Lists;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

/// <summary>Lot G-5 : écran « Transferts à envoyer », garde de la tuile, et verrou des commandes d'arrivée.</summary>
public class GroupTransfersViewModelTests
{
    private static GroupTransfer Transfer(int id = 12, string state = "requested") => new()
    {
        Id = id,
        State = state,
        Target = "Boutique de Milan",
        Lines = new List<GroupTransferLine> { new() { Line = 1, SourceVariantId = 501, Quantity = 3, Name = "Robe lin" } },
    };

    private static ApiException Refused(string code, HttpStatusCode status, string extra = "") =>
        new(ApiError.FromHttpResponse($$"""{ "code": "{{code}}"{{extra}} }""", status, null), status);

    private static (GroupTransfersViewModel Vm, FakeGroupTransferService Service, FakeDialogService Dialog) Create()
    {
        var service = new FakeGroupTransferService { Transfers = new List<GroupTransfer> { Transfer() } };
        var dialog = new FakeDialogService();
        var vm = new GroupTransfersViewModel(service, dialog)
        {
            DelayAsync = (_, _) => Task.CompletedTask,
        };
        return (vm, service, dialog);
    }

    private static Task Send(GroupTransfersViewModel vm, GroupTransferItem item) =>
        ((IAsyncRelayCommand<GroupTransferItem>)vm.SendCommand).ExecuteAsync(item);

    // ── Liste ──

    [Fact]
    public async Task Load_ShowsTransfers_AndEmptyState()
    {
        var (vm, service, _) = Create();
        service.Transfers.Add(Transfer(13, "sending"));

        await vm.AppearingAsync();

        Assert.Equal(2, vm.Items.Count);
        Assert.False(vm.IsEmpty);
        Assert.Equal("Vers Boutique de Milan", vm.Items[0].TargetText);
        Assert.Equal("À envoyer", vm.Items[0].StateText);
        Assert.True(vm.Items[0].CanSend);
        Assert.Equal("Envoyer", vm.Items[0].SendText);
        Assert.Equal("Envoi en cours", vm.Items[1].StateText);
        Assert.False(vm.Items[1].CanSend);

        service.Transfers.Clear();
        await vm.LoadAsync();
        Assert.Empty(vm.Items);
        Assert.True(vm.IsEmpty);
    }

    [Fact]
    public async Task Load_SendFailedState_ProposesResend()
    {
        var (vm, service, _) = Create();
        service.Transfers = new List<GroupTransfer> { Transfer(14, "send_failed") };

        await vm.LoadAsync();

        Assert.True(vm.Items[0].CanSend);
        Assert.Equal("Renvoyer", vm.Items[0].SendText);
    }

    // ── Envoi ──

    [Fact]
    public async Task Send_AsksConfirmation_DeclinedDoesNothing()
    {
        var (vm, service, dialog) = Create();
        dialog.ConfirmResult = false;
        await vm.LoadAsync();

        await Send(vm, vm.Items[0]);

        var confirm = Assert.Single(dialog.ConfirmCalls);
        Assert.Equal("Le stock de ces articles sortira de votre boutique maintenant.", confirm.Message);
        Assert.Empty(service.SendCalls);
    }

    [Fact]
    public async Task Send_Success_InformsAndReloads()
    {
        var (vm, service, dialog) = Create();
        await vm.LoadAsync();
        service.TransferResults.Enqueue(new List<GroupTransfer>());

        await Send(vm, vm.Items[0]);

        Assert.Equal(new[] { 12 }, service.SendCalls);
        Assert.Contains(GroupTransfersViewModel.SentMessage, dialog.ShownInfos);
        Assert.Empty(vm.Items);
        Assert.True(vm.IsEmpty);
    }

    [Fact]
    public async Task Send_StockShort_ShowsTable_AndKeepsSend()
    {
        var (vm, service, _) = Create();
        await vm.LoadAsync();
        service.SendException = Refused("TRANSFER_STOCK_SHORT", HttpStatusCode.Conflict,
            """, "lines": [ { "variantId": 501, "requested": 3, "available": 1 } ]""");

        var item = vm.Items[0];
        await Send(vm, item);

        Assert.True(item.HasNotice);
        Assert.True(item.HasShortLines);
        var line = Assert.Single(item.ShortLines);
        Assert.Equal("Robe lin", line.Label);
        Assert.Equal(3, line.Requested);
        Assert.Equal(1, line.Available);
        Assert.True(item.CanSend);
        Assert.Equal("Envoyer", item.SendText);
        Assert.Equal(1, service.GetCalls);
    }

    [Fact]
    public async Task Send_StockBusy_ProposesRetry_WhichSendsAgain()
    {
        var (vm, service, _) = Create();
        await vm.LoadAsync();
        service.SendException = Refused("TRANSFER_STOCK_BUSY", HttpStatusCode.Conflict);

        var item = vm.Items[0];
        await Send(vm, item);

        Assert.True(item.CanSend);
        Assert.Equal("Réessayer", item.SendText);
        Assert.Equal("Le stock de ces articles est en cours de modification. Réessayez dans un instant.", item.NoticeText);

        service.SendException = null;
        await Send(vm, item);
        Assert.Equal(new[] { 12, 12 }, service.SendCalls);
    }

    [Fact]
    public async Task Send_SendFailed_ProposesResend()
    {
        var (vm, service, _) = Create();
        await vm.LoadAsync();
        service.SendException = Refused("TRANSFER_SEND_FAILED", HttpStatusCode.ServiceUnavailable);

        var item = vm.Items[0];
        await Send(vm, item);

        Assert.True(item.CanSend);
        Assert.Equal("Renvoyer", item.SendText);
        Assert.Contains("le stock est revenu chez vous", item.NoticeText);
    }

    [Theory]
    [InlineData("TRANSFER_ALREADY_SENDING")]
    [InlineData("TRANSFER_ALREADY_SENT")]
    [InlineData("TRANSFER_CANCELLED")]
    public async Task Send_AlreadySendingSentOrCancelled_ReloadsList(string code)
    {
        var (vm, service, dialog) = Create();
        await vm.LoadAsync();
        service.SendException = Refused(code, HttpStatusCode.Conflict);
        service.TransferResults.Enqueue(new List<GroupTransfer>());

        await Send(vm, vm.Items[0]);

        Assert.Equal(2, service.GetCalls);
        Assert.Single(dialog.ShownInfos);
        Assert.Empty(vm.Items);
    }

    [Fact]
    public async Task Send_OtherRefusal_ShowsItsText()
    {
        var (vm, service, _) = Create();
        await vm.LoadAsync();
        service.SendException = Refused("TRANSFER_ASSOCIATED_SHOP", (HttpStatusCode)422);

        var item = vm.Items[0];
        await Send(vm, item);

        Assert.Equal("Seules les boutiques de la même entreprise que le siège peuvent échanger du stock.", item.NoticeText);
        Assert.Equal("Envoyer", item.SendText);
        Assert.Equal(1, service.GetCalls);
    }

    // ── TRANSFER_SEND_PENDING : aucun bouton, relecture bornée ──

    [Fact]
    public async Task Send_Pending_NoButton_PollsEveryFiveSeconds_AtMostTwoMinutes()
    {
        var (vm, service, _) = Create();
        var now = new DateTime(2026, 11, 19, 10, 0, 0, DateTimeKind.Utc);
        var delays = new List<TimeSpan>();
        vm.Now = () => now;
        vm.DelayAsync = (delay, _) =>
        {
            delays.Add(delay);
            now += delay;
            return Task.CompletedTask;
        };
        await vm.AppearingAsync();
        service.SendException = Refused("TRANSFER_SEND_PENDING", HttpStatusCode.ServiceUnavailable);
        // Le serveur vérifie toujours : le transfert reste « Envoi en cours ».
        service.Transfers = new List<GroupTransfer> { Transfer(12, "sending") };

        var item = vm.Items[0];
        await Send(vm, item);
        Assert.False(item.CanSend);
        Assert.Equal("Vérification en cours, revenez plus tard.", item.NoticeText);

        Assert.NotNull(vm.PollingTask);
        await vm.PollingTask!;

        Assert.Equal(24, vm.PollCount);
        Assert.All(delays, delay => Assert.Equal(TimeSpan.FromSeconds(5), delay));
        Assert.Equal(1 + 24, service.GetCalls);
        Assert.Single(service.SendCalls);
        Assert.False(vm.Items[0].CanSend);
        Assert.True(vm.Items[0].IsPending);
    }

    [Fact]
    public async Task Send_Pending_StopsWhenLeavingScreen()
    {
        var (vm, service, _) = Create();
        await vm.AppearingAsync();
        service.SendException = Refused("TRANSFER_SEND_PENDING", HttpStatusCode.ServiceUnavailable);
        var calls = 0;
        vm.DelayAsync = (_, _) =>
        {
            if (++calls == 3)
                vm.Disappearing();
            return Task.CompletedTask;
        };

        await Send(vm, vm.Items[0]);
        await vm.PollingTask!;

        Assert.Equal(2, vm.PollCount);
    }

    [Fact]
    public async Task Send_Pending_StopsOnceTransferIsSent()
    {
        var (vm, service, _) = Create();
        await vm.AppearingAsync();
        service.SendException = Refused("TRANSFER_SEND_PENDING", HttpStatusCode.ServiceUnavailable);
        service.TransferResults.Enqueue(new List<GroupTransfer> { Transfer(12, "sending") });
        service.TransferResults.Enqueue(new List<GroupTransfer>());

        await Send(vm, vm.Items[0]);
        await vm.PollingTask!;

        Assert.Equal(2, vm.PollCount);
        Assert.Empty(vm.Items);
    }

    [Fact]
    public async Task Send_Pending_ThenFailed_ProposesResend()
    {
        var (vm, service, _) = Create();
        await vm.AppearingAsync();
        service.SendException = Refused("TRANSFER_SEND_PENDING", HttpStatusCode.ServiceUnavailable);
        service.TransferResults.Enqueue(new List<GroupTransfer> { Transfer(12, "send_failed") });

        await Send(vm, vm.Items[0]);
        await vm.PollingTask!;

        Assert.Equal(1, vm.PollCount);
        Assert.False(vm.Items[0].IsPending);
        Assert.True(vm.Items[0].CanSend);
        Assert.Equal("Renvoyer", vm.Items[0].SendText);
    }

    // ── Garde de la tuile dans Gestion ──

    private sealed class Provider : IServiceProvider
    {
        private readonly object? _service;
        public Provider(object? service) => _service = service;
        public object? GetService(Type serviceType) => serviceType == typeof(Easymakemoney.Services.IGroupService) ? _service : null;
    }

    private static AllListsViewModel AllLists(object? groupService) =>
        new(null!, null!, new Provider(groupService), new FakeDialogService());

    [Fact]
    public async Task Tile_HiddenWhenGroupIsNull()
    {
        var vm = AllLists(new FakeGroupService { IsInGroup = false });
        await vm.LoadGroupAsync();
        Assert.False(vm.CanSendTransfers);
    }

    [Fact]
    public async Task Tile_ShownWhenGroupIsNotNull_AndOpensScreen()
    {
        var group = new FakeGroupService { IsInGroup = true };
        var routes = new List<string>();
        var vm = AllLists(group);
        vm.NavigateAsync = route => { routes.Add(route); return Task.CompletedTask; };

        await vm.LoadGroupAsync();
        await ((IAsyncRelayCommand)vm.NavigateToGroupTransfersCommand).ExecuteAsync(null);

        Assert.True(vm.CanSendTransfers);
        Assert.Equal(1, group.Calls);
        Assert.Equal(new[] { "GroupTransfersPage" }, routes);
    }

    [Fact]
    public async Task Tile_HiddenOnError()
    {
        var vm = AllLists(new FakeGroupService { IsInGroup = true });
        await vm.LoadGroupAsync();
        Assert.True(vm.CanSendTransfers);

        var failing = AllLists(new FakeGroupService { Exception = ApiException.NoConnection() });
        await failing.LoadGroupAsync();
        Assert.False(failing.CanSendTransfers);

        var noService = AllLists(null);
        await noService.LoadGroupAsync();
        Assert.False(noService.CanSendTransfers);
    }

    // ── Destination : écrans d'une commande d'arrivée ──

    private static ListCommand TransferOrder(int id = 30) => new()
    {
        id = id,
        name = "Transfert depuis Lyon",
        photo = "",
        date = "2026-11-18",
        groupTransfer = true,
    };

    [Fact]
    public async Task CommandList_TransferOrder_CannotBeDeletedNorMovedToTrip()
    {
        var commandService = new FakeListCommandService();
        commandService.Commands.Add(TransferOrder());
        var tripService = new FakeTripService();
        var dialog = new FakeDialogService { ConfirmResult = true };
        var vm = new ListNewCommandViewModel(
            new GetListCommandUseCase(commandService), null!, new DeleteCommandUseCase(commandService), null!, dialog, tripService)
        {
            CollectionId = 5,
        };

        await vm.GetListCommandAsync();
        var order = Assert.Single(vm.ListCommands);
        Assert.True(vm.CanEdit);
        Assert.False(order.CanEdit);

        await ((IAsyncRelayCommand<ListCommand>)vm.DeleteCommandCommand).ExecuteAsync(order);
        await ((IAsyncRelayCommand<ListCommand>)vm.ChooseTripCommand).ExecuteAsync(order);

        Assert.Empty(commandService.DeleteCalls);
        Assert.Empty(dialog.ConfirmCalls);
        Assert.Empty(tripService.GetTripsCalls);
    }

    [Fact]
    public async Task ProductList_TransferOrder_HidesEdits()
    {
        var productService = new FakeListProductsService();
        var attachmentService = new FakeProductAttachmentService();
        var dialog = new FakeDialogService { ConfirmResult = true };
        var vm = new ListNewProductViewModel(
            new GetListProductsUseCase(productService), null!, null!, new DeleteProductsUseCase(productService),
            new DetachProductUseCase(attachmentService), dialog)
        {
            CommandId = 30,
            IsGroupTransfer = true,
        };
        var product = new ListProduct { id = 100, name = "Robe lin" };

        Assert.False(vm.CanEdit);
        await ((IAsyncRelayCommand<ListProduct>)vm.DetachProductCommand).ExecuteAsync(product);
        await ((IAsyncRelayCommand<ListProduct>)vm.DeleteProductCommand).ExecuteAsync(product);

        Assert.Empty(attachmentService.DetachCalls);
        Assert.Empty(dialog.ConfirmCalls);

        vm.IsGroupTransfer = false;
        Assert.True(vm.CanEdit);
    }

    [Fact]
    public async Task DashboardCommand_TransferOrder_ShowsNotice_KeepsLabels_AndLocksProducts()
    {
        var dashService = new FakeDashboardCommandServices();
        var commandService = new FakeListCommandService();
        var dialog = new FakeDialogService();
        var routes = new List<string>();
        var vm = new DashBoardCommandViewModel(
            new GetDashboardCommandUseCase(dashService), null!, null!, null!, new GetListCommandUseCase(commandService), dialog)
        {
            NavigateAsync = route => { routes.Add(route); return Task.CompletedTask; },
        };

        await vm.SelectCommandAsync(TransferOrder(30));
        Assert.True(vm.IsGroupTransfer);

        // La réception reste : les étiquettes de ce qui est reçu s'ouvrent.
        await ((IAsyncRelayCommand)vm.OpenLabelsCommand).ExecuteAsync(null);
        await ((IAsyncRelayCommand)vm.ItemTappedCommand).ExecuteAsync(null);

        Assert.Equal(new[] { "ReceptionLabelsPage?CommandeId=30", "DashBoardProductPage?CommandId=30&Transfer=true" }, routes);

        var productVm = new DashBoardProductViewModel(null!, null!, null!, null!, dialog) { CommandId = 30, IsGroupTransfer = true };
        await productVm.ShowBottomSheetCreateProduct();
        Assert.Equal(TransferOrderRules.Notice, Assert.Single(dialog.ShownInfos));
    }

    [Fact]
    public async Task ToReceive_TransferOrder_StillReceives()
    {
        var reception = new FakeCommandeReceptionService
        {
            ReceiveResult = new CommandeReception { ReceivedQuantity = 3, ReceivedLines = 1, Remaining = 0, ReceptionStatus = "complete", GroupTransfer = true },
        };
        var dialog = new FakeDialogService { ConfirmResult = true };
        var vm = new ToReceiveViewModel(new FakeToReceiveService(), reception, dialog);
        var group = new ToReceiveGroup(30, "Transfert depuis Lyon", "2026-11-18", new List<ToReceiveLine>(), groupTransfer: true);

        await ((IAsyncRelayCommand<ToReceiveGroup>)vm.ReceiveAllCommand).ExecuteAsync(group);

        Assert.Equal(30, Assert.Single(reception.ReceiveCalls).CommandeId);
    }
}
