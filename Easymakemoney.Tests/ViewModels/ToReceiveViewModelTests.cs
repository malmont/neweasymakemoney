using CommunityToolkit.Mvvm.Input;
using Easymakemoney.Models;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

public class ToReceiveViewModelTests
{
    [Fact]
    public async Task Load_PopulatesGroups_AndSetsEmptyState()
    {
        var fakeToReceive = new FakeToReceiveService
        {
            Lines = new List<ToReceiveLine>
            {
                new ToReceiveLine { id = 1, commandeId = 10, remaining = 5, commande = new ToReceiveCommande { id = 10, name = "Commande A" } },
                new ToReceiveLine { id = 2, commandeId = 10, remaining = 3, commande = new ToReceiveCommande { id = 10, name = "Commande A" } },
                new ToReceiveLine { id = 3, commandeId = 20, remaining = 2, commande = new ToReceiveCommande { id = 20, name = "Commande B" } }
            }
        };
        var fakeReception = new FakeCommandeReceptionService();
        var fakeDialog = new FakeDialogService();

        var vm = new ToReceiveViewModel(fakeToReceive, fakeReception, fakeDialog);

        await vm.LoadAsync();

        Assert.Equal(2, vm.Groups.Count);
        Assert.False(vm.IsEmpty);
        Assert.Equal(10, vm.Groups[0].CommandeId);
        Assert.Equal(8, vm.Groups[0].Remaining);
        Assert.Equal(20, vm.Groups[1].CommandeId);
        Assert.Equal(2, vm.Groups[1].Remaining);

        // Cas vide
        fakeToReceive.Lines.Clear();
        await vm.LoadAsync();
        Assert.Empty(vm.Groups);
        Assert.True(vm.IsEmpty);
    }

    [Fact]
    public async Task ReceiveAll_Declined_DoesNotCallService()
    {
        var fakeToReceive = new FakeToReceiveService();
        var fakeReception = new FakeCommandeReceptionService();
        var fakeDialog = new FakeDialogService { ConfirmResult = false };

        var vm = new ToReceiveViewModel(fakeToReceive, fakeReception, fakeDialog);
        var group = new ToReceiveGroup(10, "Commande A", "2026-11-10", new List<ToReceiveLine>());

        await ((IAsyncRelayCommand<ToReceiveGroup>)vm.ReceiveAllCommand).ExecuteAsync(group);

        Assert.Empty(fakeReception.ReceiveCalls);
    }

    [Fact]
    public async Task ReceiveAll_WithoutNeedsVariant_CompletesAndRefreshes()
    {
        var fakeToReceive = new FakeToReceiveService();
        var fakeReception = new FakeCommandeReceptionService();
        var fakeDialog = new FakeDialogService { ConfirmResult = true };

        var firstResult = CommandeReception.Read(JObject.Parse("""
        {
            "received": [ { "lineId": 1, "quantity": 5 } ],
            "needsVariant": [],
            "commande": { "id": 10, "remaining": 0, "receptionStatus": "complete" }
        }
        """));
        fakeReception.ReceiveResults.Enqueue(firstResult);

        var vm = new ToReceiveViewModel(fakeToReceive, fakeReception, fakeDialog);
        var group = new ToReceiveGroup(10, "Commande A", "2026-11-10", new List<ToReceiveLine>());

        await ((IAsyncRelayCommand<ToReceiveGroup>)vm.ReceiveAllCommand).ExecuteAsync(group);

        Assert.Single(fakeReception.ReceiveCalls);
        Assert.Equal(10, fakeReception.ReceiveCalls[0].CommandeId);
        Assert.Null(fakeReception.ReceiveCalls[0].Body);

        Assert.Single(fakeDialog.ShownInfos);
        Assert.Contains("5 articles ajoutés au stock", fakeDialog.ShownInfos[0]);
        Assert.Contains("entièrement reçue", fakeDialog.ShownInfos[0]);
        Assert.Single(fakeToReceive.GetLinesCalls);
    }

    [Fact]
    public async Task ReceiveAll_WithNeedsVariant_VariantChosen_SendsSecondReceive()
    {
        var fakeToReceive = new FakeToReceiveService();
        var fakeReception = new FakeCommandeReceptionService();
        var fakeDialog = new FakeDialogService { ConfirmResult = true };

        var firstResult = CommandeReception.Read(JObject.Parse("""
        {
            "received": [ { "lineId": 1, "quantity": 2 } ],
            "needsVariant": [
                {
                    "lineId": 101,
                    "product": { "id": 5, "name": "Pull" },
                    "variants": [
                        { "id": 201, "size": "M", "color": "Rouge" },
                        { "id": 202, "size": "L", "color": "Bleu" }
                    ]
                }
            ],
            "commande": { "id": 10, "remaining": 1, "receptionStatus": "partial" }
        }
        """));

        var secondResult = CommandeReception.Read(JObject.Parse("""
        {
            "received": [ { "lineId": 101, "quantity": 1 } ],
            "needsVariant": [],
            "commande": { "id": 10, "remaining": 0, "receptionStatus": "complete" }
        }
        """));

        fakeReception.ReceiveResults.Enqueue(firstResult);
        fakeReception.ReceiveResults.Enqueue(secondResult);

        var vm = new ToReceiveViewModel(fakeToReceive, fakeReception, fakeDialog)
        {
            // Simule la sélection de la première variante par le gérant ("M Rouge")
            ChooseAsync = (title, labels) => Task.FromResult<string?>(labels[0])
        };

        var group = new ToReceiveGroup(10, "Commande A", "2026-11-10", new List<ToReceiveLine>());
        await ((IAsyncRelayCommand<ToReceiveGroup>)vm.ReceiveAllCommand).ExecuteAsync(group);

        Assert.Equal(2, fakeReception.ReceiveCalls.Count);
        Assert.Null(fakeReception.ReceiveCalls[0].Body);

        var secondBody = fakeReception.ReceiveCalls[1].Body;
        Assert.NotNull(secondBody);
        var lines = (JArray)secondBody!["lines"]!;
        Assert.Single(lines);
        Assert.Equal(101, (int)lines[0]["lineId"]!);
        Assert.Equal(201, (int)lines[0]["variantId"]!);

        // Cumul des quantités reçues : 2 (premier) + 1 (second) = 3
        Assert.Single(fakeDialog.ShownInfos);
        Assert.Contains("3 articles ajoutés au stock", fakeDialog.ShownInfos[0]);
        Assert.Contains("entièrement reçue", fakeDialog.ShownInfos[0]);
    }

    [Fact]
    public async Task ReceiveAll_WithNeedsVariant_LaterChosen_PostponesAndShowsSummary()
    {
        var fakeToReceive = new FakeToReceiveService();
        var fakeReception = new FakeCommandeReceptionService();
        var fakeDialog = new FakeDialogService { ConfirmResult = true };

        var firstResult = CommandeReception.Read(JObject.Parse("""
        {
            "received": [ { "lineId": 1, "quantity": 2 } ],
            "needsVariant": [
                {
                    "lineId": 101,
                    "product": { "id": 5, "name": "Pull" },
                    "variants": [
                        { "id": 201, "size": "M", "color": "Rouge" }
                    ]
                }
            ],
            "commande": { "id": 10, "remaining": null, "receptionStatus": null }
        }
        """));
        fakeReception.ReceiveResults.Enqueue(firstResult);

        var vm = new ToReceiveViewModel(fakeToReceive, fakeReception, fakeDialog)
        {
            // Le gérant choisit "Plus tard"
            ChooseAsync = (title, labels) => Task.FromResult<string?>("Plus tard")
        };

        var group = new ToReceiveGroup(10, "Commande A", "2026-11-10", new List<ToReceiveLine>());
        await ((IAsyncRelayCommand<ToReceiveGroup>)vm.ReceiveAllCommand).ExecuteAsync(group);

        // Seul le premier appel est effectué
        Assert.Single(fakeReception.ReceiveCalls);
        Assert.Null(fakeReception.ReceiveCalls[0].Body);

        // Le résumé mentionne que certains articles restent à recevoir
        Assert.Single(fakeDialog.ShownInfos);
        Assert.Contains("2 articles ajoutés au stock", fakeDialog.ShownInfos[0]);
        Assert.Contains("Certains articles restent à recevoir", fakeDialog.ShownInfos[0]);
    }
}
