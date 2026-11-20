using System.Collections.ObjectModel;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Lot G-5 : transferts de stock entre boutiques du groupe (lecture des modèles et règles pures).</summary>
public class GroupTransferTests
{
    // ── Destination : la marque groupTransfer sur les commandes fournisseurs ──

    [Fact]
    public void ListCommand_ReadsGroupTransfer_FromPeriodList()
    {
        var commandes = JsonConvert.DeserializeObject<ObservableCollection<ListCommand>>("""
        [
            { "id": 1, "name": "Transfert depuis Lyon", "photo": "", "date": "2026-11-18", "groupTransfer": true },
            { "id": 2, "name": "Commande Milan", "photo": "", "date": "2026-11-18", "groupTransfer": false },
            { "id": 3, "name": "Ancienne réponse", "photo": "", "date": "2026-11-18" }
        ]
        """)!;

        Assert.True(commandes[0].groupTransfer);
        Assert.False(commandes[1].groupTransfer);
        Assert.False(commandes[2].groupTransfer);
    }

    [Fact]
    public void ListCommand_GroupTransfer_IsNeverSent()
    {
        var json = JsonConvert.SerializeObject(new ListCommand { name = "A", photo = "", date = "2026-11-18", groupTransfer = true, PeriodClosed = true });

        Assert.DoesNotContain("groupTransfer", json);
        Assert.DoesNotContain("PeriodClosed", json);
        Assert.DoesNotContain("CanEdit", json);
    }

    [Fact]
    public void Rule_TransferOrder_HidesEditAndDelete_ButReceptionStays()
    {
        Assert.True(TransferOrderRules.CanEdit(periodClosed: false, groupTransfer: false));
        Assert.False(TransferOrderRules.CanEdit(periodClosed: false, groupTransfer: true));
        Assert.False(TransferOrderRules.CanEdit(periodClosed: true, groupTransfer: false));
        Assert.False(TransferOrderRules.CanEdit(periodClosed: true, groupTransfer: true));

        var transfer = new ListCommand { name = "T", photo = "", date = "2026-11-18", groupTransfer = true };
        var normal = new ListCommand { name = "N", photo = "", date = "2026-11-18" };
        Assert.False(transfer.CanEdit);
        Assert.True(normal.CanEdit);
        normal.PeriodClosed = true;
        Assert.False(normal.CanEdit);

        // Le paramètre de navigation n'apparaît que pour un transfert : les autres routes ne changent pas.
        Assert.Equal("&Transfer=true", TransferOrderRules.RouteSuffix(true));
        Assert.Equal(string.Empty, TransferOrderRules.RouteSuffix(false));
    }

    [Fact]
    public void FraisDePortRow_OfTransferOrder_IsReadOnly()
    {
        var transfer = new ListCommand { id = 1, name = "T", photo = "", date = "2026-11-18", groupTransfer = true };
        var withFrais = new FraisDePortCommandeRow { Commande = transfer, Frais = new FraisDePort { id = 1, name = "Port", image = "", price = 900 } };
        var withoutFrais = new FraisDePortCommandeRow { Commande = transfer };

        Assert.True(withFrais.IsGroupTransfer);
        Assert.False(withFrais.CanEditFrais);
        Assert.False(withFrais.CanDeleteFrais);
        Assert.False(withoutFrais.CanAddFrais);
    }

    [Fact]
    public void CommandeReception_ReadsGroupTransfer_FromCommandeBlock()
    {
        var transfer = CommandeReception.Read(JObject.Parse("""
        { "received": [{ "lineId": 1, "quantity": 2 }], "commande": { "id": 9, "remaining": 0, "receptionStatus": "complete", "groupTransfer": true } }
        """));
        var normal = CommandeReception.Read(JObject.Parse("""
        { "received": [], "commande": { "id": 9, "remaining": 3 } }
        """));

        Assert.True(transfer.GroupTransfer);
        Assert.Equal(2, transfer.ReceivedQuantity);
        Assert.False(normal.GroupTransfer);
    }

    [Fact]
    public void ToReceiveGroup_KeepsGroupTransfer_WhenTheRouteGivesIt()
    {
        var lines = JsonConvert.DeserializeObject<List<ToReceiveLine>>("""
        [
            { "id": 1, "commandeId": 4, "remaining": 2, "commande": { "id": 4, "name": "Transfert depuis Lyon", "groupTransfer": true } },
            { "id": 2, "commandeId": 5, "remaining": 1, "commande": { "id": 5, "name": "Commande Milan" } }
        ]
        """)!;

        var groups = ToReceiveGroup.Build(lines);

        Assert.True(groups[0].GroupTransfer);
        Assert.False(groups[1].GroupTransfer);
    }

    [Fact]
    public void ApiError_TransferOrderLocked_HasFrenchText()
    {
        var error = ApiError.FromHttpResponse("""{ "code": "TRANSFER_ORDER_LOCKED" }""", HttpStatusCode.Conflict, null);

        Assert.Equal("Cette commande vient d'un transfert entre boutiques du groupe : elle ne se modifie pas et ne se supprime pas. Vous pouvez la réceptionner.", error.Message);
    }

    // ── Source : la liste des transferts à envoyer ──

    [Fact]
    public void ReadList_ReadsTransfersLinesAndStates()
    {
        var transfers = GroupTransfer.ReadList(JObject.Parse("""
        {
            "transfers": [
                {
                    "id": 12, "source": "lyon", "target": { "id": 4, "name": "Boutique de Milan" }, "state": "requested", "currency": "EUR",
                    "requestedAt": "2026-11-18T09:30:00+00:00", "partlyReceived": false,
                    "lines": [
                        { "line": 1, "sourceVariantId": 501, "targetVariantId": 901, "quantity": 3 },
                        { "line": 2, "sourceVariantId": 502, "targetVariantId": 902, "quantity": 1, "name": "Robe lin · Taille M" }
                    ]
                },
                { "id": 13, "target": "Boutique de Nice", "state": "sending", "lines": [] },
                { "id": 14, "target": 7, "state": "send_failed", "lines": [] },
                { "state": "requested" }
            ]
        }
        """));

        Assert.Equal(3, transfers.Count);
        var first = transfers[0];
        Assert.Equal(12, first.Id);
        Assert.Equal("Vers Boutique de Milan", first.TargetText);
        Assert.Equal("À envoyer", first.StateText);
        Assert.StartsWith("Demandé le 18 novembre 2026", first.RequestedText);
        Assert.True(first.IsSendable);
        Assert.Equal(2, first.Lines.Count);
        Assert.Equal("Variante n° 501", first.Lines[0].Label);
        Assert.Equal("3 articles", first.Lines[0].QuantityText);
        Assert.Equal("Robe lin · Taille M", first.Lines[1].Label);
        Assert.Equal("1 article", first.Lines[1].QuantityText);

        Assert.Equal("Vers Boutique de Nice", transfers[1].TargetText);
        Assert.Equal("Envoi en cours", transfers[1].StateText);
        Assert.False(transfers[1].IsSendable);
        Assert.Equal("Vers Boutique n° 7", transfers[2].TargetText);
        Assert.Equal("Échec de l'envoi", transfers[2].StateText);
        Assert.True(transfers[2].IsSendable);
    }

    [Fact]
    public void ReadList_EmptyOrInvalid_ReturnsEmpty()
    {
        Assert.Empty(GroupTransfer.ReadList(null));
        Assert.Empty(GroupTransfer.ReadList(JObject.Parse("""{ "transfers": [] }""")));
        Assert.Empty(GroupTransfer.ReadList(JObject.Parse("""{ "other": 1 }""")));
    }

    [Fact]
    public async Task Service_ReadsRoutes()
    {
        var http = new FakeHttpService { ResultToReturn = JObject.Parse("""{ "transfers": [{ "id": 3, "state": "requested", "lines": [] }] }""") };
        var service = new GroupTransferService(http);

        var transfers = await service.GetTransfersToSendAsync();
        await service.SendAsync(3);

        Assert.Equal("site/group-transfers", Assert.Single(http.RequestedUrls));
        Assert.Equal(3, Assert.Single(transfers).Id);
        Assert.Equal("site/group-transfers/3/send", Assert.Single(http.PostedAuthData).Url);
    }

    // ── Les refus de l'envoi ──

    private static TransferSendRefusal Refusal(string json, HttpStatusCode status, IEnumerable<GroupTransferLine>? lines = null) =>
        TransferSendRefusal.From(ApiError.FromHttpResponse(json, status, null), status, lines);

    [Fact]
    public void Refusal_StockShort_GivesRequestedAvailableTable()
    {
        var lines = new[]
        {
            new GroupTransferLine { Line = 1, SourceVariantId = 501, Quantity = 3, Name = "Robe lin" },
        };
        var refusal = Refusal("""
        { "code": "TRANSFER_STOCK_SHORT", "lines": [ { "variantId": 501, "requested": 3, "available": 1 }, { "variantId": 777, "requested": 2, "available": 0 } ] }
        """, HttpStatusCode.Conflict, lines);

        Assert.Equal(TransferSendAction.None, refusal.Action);
        Assert.Equal(string.Empty, refusal.ActionText);
        Assert.Equal("Stock insuffisant dans votre boutique : rien n'est sorti. Voici ce qui manque.", refusal.Message);
        Assert.True(refusal.HasShortLines);
        Assert.Equal(2, refusal.ShortLines.Count);
        Assert.Equal("Robe lin", refusal.ShortLines[0].Label);
        Assert.Equal("3", refusal.ShortLines[0].RequestedText);
        Assert.Equal("1", refusal.ShortLines[0].AvailableText);
        Assert.Equal("Variante n° 777", refusal.ShortLines[1].Label);
    }

    [Fact]
    public void Refusal_StockBusy_ProposesRetry()
    {
        var refusal = Refusal("""{ "code": "TRANSFER_STOCK_BUSY" }""", HttpStatusCode.Conflict);

        Assert.Equal(TransferSendAction.Retry, refusal.Action);
        Assert.Equal("Réessayer", refusal.ActionText);
        Assert.Equal("Le stock de ces articles est en cours de modification. Réessayez dans un instant.", refusal.Message);
    }

    [Fact]
    public void Refusal_SendFailed_ProposesResend_WithFrenchTextOverServerText()
    {
        var refusal = Refusal("""{ "code": "TRANSFER_SEND_FAILED", "message": "Target shop refused" }""", HttpStatusCode.ServiceUnavailable);

        Assert.Equal(TransferSendAction.Resend, refusal.Action);
        Assert.Equal("Renvoyer", refusal.ActionText);
        Assert.Equal("La boutique de destination a refusé l'envoi : le stock est revenu chez vous. Vous pouvez renvoyer.", refusal.Message);
    }

    [Fact]
    public void Refusal_SendPending_NoButton_AndWaits()
    {
        var refusal = Refusal("""{ "code": "TRANSFER_SEND_PENDING" }""", HttpStatusCode.ServiceUnavailable);

        Assert.Equal(TransferSendAction.Wait, refusal.Action);
        Assert.Equal(string.Empty, refusal.ActionText);
        Assert.Equal("Vérification en cours, revenez plus tard.", refusal.Message);
    }

    [Theory]
    [InlineData("TRANSFER_ALREADY_SENDING", "Ce transfert est déjà en cours d'envoi.")]
    [InlineData("TRANSFER_ALREADY_SENT", "Ce transfert a déjà été envoyé.")]
    [InlineData("TRANSFER_CANCELLED", "Ce transfert a été annulé par le siège.")]
    public void Refusal_AlreadySendingSentOrCancelled_ReloadsList(string code, string message)
    {
        var refusal = Refusal($$"""{ "code": "{{code}}" }""", HttpStatusCode.Conflict);

        Assert.Equal(TransferSendAction.Reload, refusal.Action);
        Assert.Equal(string.Empty, refusal.ActionText);
        Assert.Equal(message, refusal.Message);
    }

    [Theory]
    [InlineData("TRANSFER_ASSOCIATED_SHOP", 422, "Seules les boutiques de la même entreprise que le siège peuvent échanger du stock.")]
    [InlineData("TRANSFER_NOT_FOUND", 404, "Ce transfert n'existe plus.")]
    [InlineData("GROUP_SHOP_UNKNOWN", 404, "Cette boutique n'est pas connue du groupe.")]
    public void Refusal_Others_ShowTheirText(string code, int status, string message)
    {
        var refusal = Refusal($$"""{ "code": "{{code}}" }""", (HttpStatusCode)status);

        Assert.Equal(TransferSendAction.None, refusal.Action);
        Assert.Equal(message, refusal.Message);
        Assert.False(refusal.HasShortLines);
    }

    [Fact]
    public void Refusal_UnknownCode_KeepsItsOwnText()
    {
        var refusal = Refusal("""{ "code": "SOMETHING_ELSE", "message": "Texte du serveur." }""", HttpStatusCode.BadRequest);

        Assert.Equal(TransferSendAction.None, refusal.Action);
        Assert.Equal("Texte du serveur.", refusal.Message);
    }

    // ── Relecture bornée après TRANSFER_SEND_PENDING ──

    [Fact]
    public void Polling_EveryFiveSeconds_AtMostTwoMinutes()
    {
        Assert.Equal(TimeSpan.FromSeconds(5), TransferPendingPolling.Interval);
        Assert.Equal(24, TransferPendingPolling.MaxAttempts);

        Assert.True(TransferPendingPolling.ShouldPollAgain(0, TimeSpan.Zero, visible: true, stillPending: true));
        Assert.True(TransferPendingPolling.ShouldPollAgain(23, TimeSpan.FromSeconds(115), visible: true, stillPending: true));
        Assert.False(TransferPendingPolling.ShouldPollAgain(24, TimeSpan.FromSeconds(115), visible: true, stillPending: true));
        Assert.False(TransferPendingPolling.ShouldPollAgain(3, TimeSpan.FromSeconds(118), visible: true, stillPending: true));
        Assert.False(TransferPendingPolling.ShouldPollAgain(3, TimeSpan.FromSeconds(15), visible: false, stillPending: true));
        Assert.False(TransferPendingPolling.ShouldPollAgain(3, TimeSpan.FromSeconds(15), visible: true, stillPending: false));
    }

    [Fact]
    public void Polling_StillPending_UntilSentOrFailed()
    {
        Assert.True(TransferPendingPolling.IsStillPending(new GroupTransfer { Id = 1, State = "sending" }));
        Assert.True(TransferPendingPolling.IsStillPending(new GroupTransfer { Id = 1, State = "requested" }));
        Assert.False(TransferPendingPolling.IsStillPending(new GroupTransfer { Id = 1, State = "send_failed" }));
        Assert.False(TransferPendingPolling.IsStillPending(null));
    }

    // ── Garde de l'écran : GET /api/group ──

    [Fact]
    public void GroupMembership_OnlyNonNullGroup()
    {
        Assert.True(GroupMembership.IsMember(JObject.Parse("""{ "group": { "id": 2, "name": "Groupe Sud" } }""")));
        Assert.False(GroupMembership.IsMember(JObject.Parse("""{ "group": null }""")));
        Assert.False(GroupMembership.IsMember(JObject.Parse("""{ }""")));
        Assert.False(GroupMembership.IsMember(null));
    }

    [Fact]
    public async Task GroupService_ReadsGroupRoute()
    {
        var http = new FakeHttpService { ResultToReturn = JObject.Parse("""{ "group": { "id": 2 } }""") };

        Assert.True(await new GroupService(http).IsInGroupAsync());
        Assert.Equal("group", Assert.Single(http.RequestedUrls));
    }
}
