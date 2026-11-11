using System.Net;
using System.Text;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class ToReceiveTests
{
    private const string Json = """
    [
      { "id": 11, "commandeId": 4, "product": { "id": 1, "name": "Robe", "code": "R1" }, "variant": { "id": 3, "size": "M", "color": "Noir" },
        "quantityOrdered": 5, "quantityReceived": 2, "remaining": 3, "commande": { "id": 4, "name": "Atelier Nord", "date": "2026-10-02" } },
      { "id": 12, "commandeId": 4, "product": { "id": 2, "name": "Parka" }, "variant": null,
        "quantityOrdered": 4, "quantityReceived": 0, "remaining": 4, "commande": { "id": 4, "name": "Atelier Nord", "date": "2026-10-02" } },
      { "id": 20, "commandeId": 7, "product": { "id": 5, "name": "Bonnet" }, "variant": { "id": 8, "size": null, "color": "Rouge" },
        "quantityOrdered": 10, "quantityReceived": 0, "remaining": 10, "commande": { "id": 7, "name": "Salon", "date": "2026-10-20" } }
    ]
    """;

    [Fact]
    public void Lines_AreGroupedByCommande_WithTotals()
    {
        var groups = ToReceiveGroup.Build(JsonConvert.DeserializeObject<List<ToReceiveLine>>(Json)!);

        Assert.Equal(2, groups.Count);
        Assert.Equal("Atelier Nord", groups[0].Name);
        Assert.Equal(7, groups[0].Remaining);
        Assert.Equal("7 articles à recevoir", groups[0].RemainingText);
        Assert.Equal("Commande du 02/10/2026", groups[0].DateText);
        Assert.Equal("M · Noir", groups[0][0].VariantText);
        Assert.Equal("3 à recevoir sur 5 (2 déjà reçus)", groups[0][0].QuantityText);
        Assert.False(groups[0][1].HasVariant);
        Assert.Equal("Rouge", groups[1][0].VariantText);
    }

    [Fact]
    public async Task Service_UsesContractRoute()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new List<ToReceiveLine>() };

        await new ToReceiveService(fakeHttp).GetLinesAsync();

        Assert.Equal("commandes/lignes/a-recevoir?page=1&limit=200", Assert.Single(fakeHttp.RequestedUrls));
    }

    [Fact]
    public async Task ToReceiveService_EmptyApiResponse_ReturnsEmptyListAndBuildsEmptyGroups()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);
        var service = new ToReceiveService(httpService);

        var lines = await service.GetLinesAsync();

        Assert.NotNull(lines);
        Assert.Empty(lines);

        var groups = ToReceiveGroup.Build(lines);
        Assert.NotNull(groups);
        Assert.Empty(groups);
    }
}
