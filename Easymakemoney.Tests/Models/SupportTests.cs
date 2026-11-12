using System.Globalization;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Models.Session;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Module Assistance, lot 1 du backend (docs/assistance.md) : modèles, service et écrans de l'appli.</summary>
[Collection("SiteCurrency")]
public class SupportTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public SupportTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
        SupportTicketListViewModel.TicketsChanged = false;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
        SupportTicketListViewModel.TicketsChanged = false;
    }

    private static string Iso(DateTime date) => new DateTimeOffset(date).ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);

    private static string DetailJson() => $$"""
    { "id": 7, "reference": "AS-000007", "subject": "Colis abîmé", "status": "open", "priority": "high", "channel": "phone",
      "customer": { "userId": null, "name": "Julie Test", "email": "julie@example.test", "locale": "fr" },
      "order": { "id": 42, "reference": "REF#42" }, "cashRegisterId": null,
      "assignedTo": { "id": 3, "name": "Marc" }, "dueAt": "{{Iso(DateTime.Today.AddDays(2))}}", "overdue": false, "awaitingReply": true,
      "firstResponseAt": null, "lastMessageAt": "{{Iso(DateTime.Today)}}", "closedAt": null, "anonymized": false, "createdAt": "{{Iso(DateTime.Today)}}",
      "messages": [
        { "id": 1, "authorType": "customer", "author": { "id": null, "name": null }, "body": "Le colis est arrivé abîmé.", "internal": false, "emailStatus": null, "createdAt": "{{Iso(DateTime.Today)}}", "attachments": [ { "id": 5, "name": "photo.jpg", "mime": "image/jpeg", "size": 1000 } ] },
        { "id": 2, "authorType": "agent", "author": { "id": 3, "name": "Marc" }, "body": "Vérifier le stock.", "internal": true, "emailStatus": null, "createdAt": "{{Iso(DateTime.Today)}}", "attachments": [] },
        { "id": 3, "authorType": "agent", "author": { "id": 3, "name": "Marc" }, "body": "Nous vous renvoyons l'article.", "internal": false, "emailStatus": "logged", "createdAt": "{{Iso(DateTime.Today)}}", "attachments": [] }
      ],
      "orderSummary": { "id": 42, "reference": "REF#42", "date": "{{Iso(DateTime.Today.AddDays(-3))}}", "source": "web", "status": "Livrée",
        "lines": [ { "name": "T-shirt", "variant": "M", "quantity": 2, "unitPrice": 2500, "discount": 0, "total": 5000 } ],
        "totals": { "subtotal": 5000, "discount": 0, "shipping": 800, "tax": 870, "total": 6670 },
        "payments": [ { "method": "Carte", "amount": 6670, "date": "{{Iso(DateTime.Today.AddDays(-3))}}" } ], "returns": [],
        "shipping": { "carrier": "Postes Canada", "trackingNumber": "123", "trackingUrl": null, "shippedAt": null } } }
    """;

    // ---------- Modèles ----------

    [Fact]
    public void Detail_ParsesServerShapeAndTexts()
    {
        var ticket = JsonConvert.DeserializeObject<SupportTicketDetail>(DetailJson())!;

        Assert.Equal("AS-000007 · En cours · Haute", ticket.HeaderText);
        Assert.True(ticket.IsUrgent);
        Assert.Equal("Julie Test · julie@example.test", ticket.CustomerText);
        Assert.Equal("Suivie par Marc", ticket.AssignedText);
        Assert.Equal("Téléphone", ticket.ChannelText);
        Assert.StartsWith("Réponse attendue avant le", ticket.DueText);
        Assert.True(ticket.HasOrder);
        Assert.Equal(3, ticket.messages.Count);
    }

    [Fact]
    public void Messages_KindTexts()
    {
        var messages = JsonConvert.DeserializeObject<SupportTicketDetail>(DetailJson())!.messages;

        Assert.Equal("Client", messages[0].AuthorText);
        Assert.True(messages[0].HasAttachments);
        Assert.Equal("photo.jpg", messages[0].AttachmentsText);
        Assert.False(messages[0].HasKind);
        Assert.True(messages[1].IsInternal);
        Assert.Equal("Note interne (jamais envoyée)", messages[1].KindText);
        Assert.Equal("Marc", messages[2].AuthorText);
        Assert.Equal("Consigné (envoi des courriels désactivé)", messages[2].KindText);
    }

    [Theory]
    [InlineData("sent", "Envoyé au client")]
    [InlineData("failed", "Courriel non envoyé")]
    [InlineData(null, "")]
    public void Message_EmailStatus(string? status, string expected)
    {
        Assert.Equal(expected, new SupportMessage { authorType = "agent", emailStatus = status }.KindText);
    }

    [Fact]
    public void Ticket_DueTexts()
    {
        Assert.Equal("Réponse en retard", new SupportTicket { status = "open", overdue = true }.DueText);
        Assert.StartsWith("Fermée", new SupportTicket { status = "closed", closedAt = Iso(DateTime.Today) }.DueText);
        Assert.Equal("Non attribuée", new SupportTicket().AssignedText);
        Assert.Equal("Client inconnu", new SupportTicket().CustomerText);
    }

    [Fact]
    public void OrderSummary_ShowsWhitelistOnly()
    {
        var summary = JsonConvert.DeserializeObject<SupportTicketDetail>(DetailJson())!.orderSummary!;

        Assert.Contains("REF#42", summary.TitleText);
        Assert.Contains("2 × T-shirt (M)", summary.LinesText);
        Assert.StartsWith("Total :", summary.TotalText);
        Assert.True(summary.HasPayments);
        Assert.False(summary.HasReturns);
        Assert.Equal("Postes Canada · 123", summary.ShippingText);
    }

    [Theory]
    [InlineData("new", new[] { "open", "closed" })]
    [InlineData("open", new[] { "waiting_customer", "closed" })]
    [InlineData("waiting_customer", new[] { "open", "closed" })]
    [InlineData("closed", new[] { "open" })]
    public void NextStatuses_FollowContract(string status, string[] expected)
    {
        Assert.Equal(expected, SupportTexts.NextStatuses(status));
    }

    [Fact]
    public void CreateRequest_Body()
    {
        var body = new SupportTicketCreateRequest
        {
            Subject = " Colis ", Body = " abîmé ", Channel = "counter", Priority = "high", CustomerName = "Julie", CustomerEmail = "julie@example.test", OrderId = 42,
        }.ToBody();

        Assert.Equal("""{"subject":"Colis","body":"abîmé","channel":"counter","priority":"high","customer":{"name":"Julie","email":"julie@example.test"},"orderId":42}""",
            JsonConvert.SerializeObject(body));
        Assert.False(new SupportTicketCreateRequest { Subject = "a", Body = "b" }.ToBody().ContainsKey("orderId"));
    }

    // ---------- Service ----------

    [Fact]
    public void Service_ListUrl()
    {
        Assert.Equal("support/tickets?page=1&pageSize=20", SupportService.ListUrl(null, null, null, 1));
        Assert.Equal("support/tickets?status=waiting_customer&assigned=me&q=AS-0007%20julie&page=2&pageSize=20",
            SupportService.ListUrl("waiting_customer", "me", " AS-0007 julie ", 2));
    }

    [Fact]
    public async Task Service_Routes()
    {
        var http = new FakeHttpService { ResultToReturn = new SupportTicket() };
        var service = new SupportService(http);

        await service.UpdateAsync(7, new Dictionary<string, object?> { ["status"] = "closed" });
        await service.AddMessageAsync(7, " Bonjour ", isInternal: true, fromCustomer: false);
        await service.CreateAsync(new SupportTicketCreateRequest { Subject = "a", Body = "b", CustomerName = "c" });

        Assert.Equal("support/tickets/7", http.PostedAuthData[0].Url);
        Assert.Equal("support/tickets/7/messages", http.PostedAuthData[1].Url);
        Assert.Equal("""{"body":"Bonjour","internal":true}""", JsonConvert.SerializeObject(http.PostedAuthData[1].Data));
        Assert.Equal("support/tickets", http.PostedAuthData[2].Url);
    }

    // ---------- Liste ----------

    [Fact]
    public async Task List_LoadsOnceThenOnlyAfterChange()
    {
        var service = new FakeSupportService();
        var vm = new SupportTicketListViewModel(service, new FakeDialogService());

        await vm.InitializeAsync();
        await vm.InitializeAsync();
        Assert.Single(service.ListCalls);

        SupportTicketListViewModel.TicketsChanged = true;
        await vm.InitializeAsync();
        Assert.Equal(2, service.ListCalls.Count);
        Assert.False(SupportTicketListViewModel.TicketsChanged);
    }

    [Fact]
    public async Task List_FilterMineAndPaging()
    {
        var service = new FakeSupportService { List = new SupportTicketList { items = { new SupportTicket { id = 1 } }, total = 30, page = 1, pageSize = 20 } };
        var vm = new SupportTicketListViewModel(service, new FakeDialogService());
        await vm.InitializeAsync();

        await vm.SelectFilterCommand.ExecuteAsync("waiting_customer");
        Assert.Equal(("waiting_customer", (string?)null, 1), (service.ListCalls[^1].Status, service.ListCalls[^1].Assigned, service.ListCalls[^1].Page));
        Assert.True(vm.HasMore);
        Assert.Equal("30 demandes", vm.TotalText);

        await vm.LoadMoreCommand.ExecuteAsync(null);
        Assert.Equal(2, service.ListCalls[^1].Page);
        Assert.Equal(2, vm.Tickets.Count);
    }

    [Fact]
    public async Task List_ModuleInactive_ShowsCardWithoutError()
    {
        var dialogs = new FakeDialogService();
        var service = new FakeSupportService
        {
            Error = new ApiException(ApiError.FromHttpResponse("""{ "code": "MODULE_INACTIVE", "module": "support", "feature": "module.support" }""", HttpStatusCode.Forbidden, null), HttpStatusCode.Forbidden),
        };
        var vm = new SupportTicketListViewModel(service, dialogs);

        await vm.InitializeAsync();

        Assert.True(vm.IsModuleInactive);
        Assert.False(vm.IsEmpty);
        Assert.Empty(dialogs.ShownErrors);
    }

    // ---------- Fiche ----------

    private static (SupportTicketViewModel Vm, FakeSupportService Service, FakeDialogService Dialogs) CreateTicketViewModel(int me = 3)
    {
        var service = new FakeSupportService { Detail = JsonConvert.DeserializeObject<SupportTicketDetail>(DetailJson())! };
        var dialogs = new FakeDialogService();
        var session = new FakeSessionService { CurrentUser = new UserInfo { Id = me } };
        var vm = new SupportTicketViewModel(service, dialogs, session) { TicketId = 7 };
        return (vm, service, dialogs);
    }

    [Fact]
    public async Task Ticket_Loads()
    {
        var (vm, _, _) = CreateTicketViewModel();

        await vm.LoadAsync();

        Assert.True(vm.HasTicket);
        Assert.True(vm.CanReply);
        Assert.Equal(3, vm.Messages.Count);
        Assert.Equal("Ne plus suivre", vm.AssignText);
    }

    [Fact]
    public async Task Send_EmptyText_NoRequest()
    {
        var (vm, service, dialogs) = CreateTicketViewModel();
        await vm.LoadAsync();

        await vm.SendCommand.ExecuteAsync(null);

        Assert.Empty(service.Messages);
        Assert.Equal("Écrivez d'abord le message.", dialogs.ShownInfos.Single());
    }

    [Fact]
    public async Task Send_Reply_AsksConfirmation_RefusedSendsNothing()
    {
        var (vm, service, dialogs) = CreateTicketViewModel();
        await vm.LoadAsync();
        dialogs.ConfirmResult = false;
        vm.ReplyText = "Bonjour";

        await vm.SendCommand.ExecuteAsync(null);

        Assert.Contains("julie@example.test", dialogs.ConfirmCalls.Single().Message);
        Assert.Empty(service.Messages);
        Assert.Equal("Bonjour", vm.ReplyText);
    }

    [Fact]
    public async Task Send_Reply_Confirmed_SendsAndReloads()
    {
        var (vm, service, _) = CreateTicketViewModel();
        await vm.LoadAsync();
        vm.ReplyText = " Bonjour ";

        await vm.SendCommand.ExecuteAsync(null);

        Assert.Equal((7, "Bonjour", false, false), service.Messages.Single());
        Assert.Equal(string.Empty, vm.ReplyText);
        Assert.Equal(2, service.DetailCalls);
        Assert.True(SupportTicketListViewModel.TicketsChanged);
    }

    [Theory]
    [InlineData(SupportTicketViewModel.ModeNote, true, false)]
    [InlineData(SupportTicketViewModel.ModeCustomer, false, true)]
    public async Task Send_NoteOrCustomer_NoConfirmation(string mode, bool isInternal, bool fromCustomer)
    {
        var (vm, service, dialogs) = CreateTicketViewModel();
        await vm.LoadAsync();
        vm.SelectModeCommand.Execute(mode);
        vm.ReplyText = "Texte";

        await vm.SendCommand.ExecuteAsync(null);

        Assert.Empty(dialogs.ConfirmCalls);
        Assert.Equal((7, "Texte", isInternal, fromCustomer), service.Messages.Single());
    }

    [Fact]
    public async Task Send_TicketClosedError_ShowsInfoNotError()
    {
        var (vm, service, dialogs) = CreateTicketViewModel();
        await vm.LoadAsync();
        vm.SelectModeCommand.Execute(SupportTicketViewModel.ModeNote);
        vm.ReplyText = "Texte";
        service.MessageError = new ApiException(ApiError.FromHttpResponse("""{ "code": "TICKET_CLOSED", "message": "x" }""", HttpStatusCode.Conflict, null), HttpStatusCode.Conflict);

        await vm.SendCommand.ExecuteAsync(null);

        Assert.Contains("fermée", dialogs.ShownInfos.Single());
        Assert.Empty(dialogs.ShownErrors);
    }

    [Fact]
    public async Task ChangeStatus_OffersOnlyAllowedTransitions()
    {
        var (vm, service, _) = CreateTicketViewModel();
        await vm.LoadAsync();
        string[]? offered = null;
        vm.ChooseAsync = (_, choices) => { offered = choices; return Task.FromResult<string?>("Attend le client"); };

        await vm.ChangeStatusCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "Attend le client", "Fermée" }, offered);
        Assert.Equal("""{"status":"waiting_customer"}""", JsonConvert.SerializeObject(service.Patches.Single()));
    }

    [Fact]
    public async Task ChangeStatus_Cancelled_SendsNothing()
    {
        var (vm, service, _) = CreateTicketViewModel();
        await vm.LoadAsync();
        vm.ChooseAsync = (_, _) => Task.FromResult<string?>("Annuler");

        await vm.ChangeStatusCommand.ExecuteAsync(null);

        Assert.Empty(service.Patches);
    }

    [Fact]
    public async Task ChangePriority_ExcludesCurrent()
    {
        var (vm, service, _) = CreateTicketViewModel();
        await vm.LoadAsync();
        string[]? offered = null;
        vm.ChooseAsync = (_, choices) => { offered = choices; return Task.FromResult<string?>("Urgente"); };

        await vm.ChangePriorityCommand.ExecuteAsync(null);

        Assert.DoesNotContain("Haute", offered!);
        Assert.Equal("""{"priority":"urgent"}""", JsonConvert.SerializeObject(service.Patches.Single()));
    }

    [Fact]
    public async Task ToggleAssign_MineUnassigns_OtherAssignsToMe()
    {
        var (mine, mineService, _) = CreateTicketViewModel(me: 3);
        await mine.LoadAsync();
        await mine.ToggleAssignCommand.ExecuteAsync(null);
        Assert.Equal("""{"assignedTo":null}""", JsonConvert.SerializeObject(mineService.Patches.Single()));

        var (other, otherService, _) = CreateTicketViewModel(me: 9);
        await other.LoadAsync();
        Assert.Equal("M'attribuer", other.AssignText);
        await other.ToggleAssignCommand.ExecuteAsync(null);
        Assert.Equal("""{"assignedTo":9}""", JsonConvert.SerializeObject(otherService.Patches.Single()));
    }

    [Fact]
    public async Task ClosedTicket_CannotReply()
    {
        var (vm, service, _) = CreateTicketViewModel();
        service.Detail.status = "closed";
        await vm.LoadAsync();
        vm.ReplyText = "x";

        await vm.SendCommand.ExecuteAsync(null);

        Assert.False(vm.CanReply);
        Assert.Empty(service.Messages);
    }

    // ---------- Création ----------

    [Theory]
    [InlineData("", "b", "Julie", "", "", "Indiquez l'objet de la demande.")]
    [InlineData("a", "", "Julie", "", "", "Décrivez la demande du client.")]
    [InlineData("a", "b", "", "", "", "Indiquez au moins le nom ou le courriel du client.")]
    [InlineData("a", "b", "", "pas-un-courriel", "", "Le courriel du client ne semble pas valide.")]
    [InlineData("a", "b", "Julie", "", "abc", "Le numéro de commande doit être un nombre.")]
    public async Task Create_InvalidInput_NoRequest(string subject, string body, string name, string email, string order, string message)
    {
        var service = new FakeSupportService();
        var dialogs = new FakeDialogService();
        var vm = new SupportTicketCreateViewModel(service, dialogs)
        {
            Subject = subject, Body = body, CustomerName = name, CustomerEmail = email, OrderIdText = order, GoBackAsync = () => Task.CompletedTask,
        };

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(service.Created);
        Assert.Equal(message, dialogs.ShownInfos.Single());
    }

    [Fact]
    public async Task Create_Valid_SendsChoicesAndGoesBack()
    {
        var service = new FakeSupportService();
        var dialogs = new FakeDialogService();
        var back = 0;
        var vm = new SupportTicketCreateViewModel(service, dialogs)
        {
            Subject = "Colis", Body = "abîmé", CustomerEmail = "julie@example.test", OrderIdText = "#42", GoBackAsync = () => { back++; return Task.CompletedTask; },
        };
        vm.SelectChannelCommand.Execute("counter");
        vm.SelectPriorityCommand.Execute("urgent");

        await vm.SaveCommand.ExecuteAsync(null);

        var request = service.Created.Single();
        Assert.Equal(("counter", "urgent", 42), (request.Channel, request.Priority, request.OrderId));
        Assert.Equal(1, back);
        Assert.Equal("Demande AS-000099 créée.", dialogs.ShownInfos.Single());
        Assert.True(SupportTicketListViewModel.TicketsChanged);
    }

    // ---------- Faux service ----------

    private sealed class FakeSupportService : ISupportService
    {
        public SupportTicketList List { get; set; } = new();
        public SupportTicketDetail Detail { get; set; } = new();
        public Exception? Error { get; set; }
        public Exception? MessageError { get; set; }
        public List<(string? Status, string? Assigned, string? Query, int Page)> ListCalls { get; } = new();
        public int DetailCalls { get; private set; }
        public List<IDictionary<string, object?>> Patches { get; } = new();
        public List<(int Id, string Body, bool Internal, bool FromCustomer)> Messages { get; } = new();
        public List<SupportTicketCreateRequest> Created { get; } = new();

        public Task<SupportTicketList> GetTicketsAsync(string? status, string? assigned, string? query, int page)
        {
            ListCalls.Add((status, assigned, query, page));
            return Error != null ? Task.FromException<SupportTicketList>(Error) : Task.FromResult(List);
        }

        public Task<SupportTicketDetail> GetTicketAsync(int id)
        {
            DetailCalls++;
            return Error != null ? Task.FromException<SupportTicketDetail>(Error) : Task.FromResult(Detail);
        }

        public Task<SupportTicket> CreateAsync(SupportTicketCreateRequest request)
        {
            Created.Add(request);
            return Task.FromResult(new SupportTicket { id = 99, reference = "AS-000099" });
        }

        public Task<SupportTicket> UpdateAsync(int id, IDictionary<string, object?> changes)
        {
            Patches.Add(changes);
            return Task.FromResult(new SupportTicket());
        }

        public Task AddMessageAsync(int id, string body, bool isInternal, bool fromCustomer)
        {
            if (MessageError != null)
                return Task.FromException(MessageError);
            Messages.Add((id, body.Trim(), isInternal, fromCustomer));
            return Task.CompletedTask;
        }
    }
}
