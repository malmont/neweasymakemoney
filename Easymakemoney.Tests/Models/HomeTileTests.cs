using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class HomeTileTests
{
    private const string Json = """
    [
      { "id": "low-stock", "kind": "alert", "priority": 80, "title": "Stock bas", "text": "2 articles vont manquer.",
        "action": { "screen": "stock.low", "params": { "variantIds": [11, 12] } },
        "sources": [ { "label": "Ventes 30 jours", "value": 9 }, { "label": "Stock", "value": 3 } ],
        "dismissible": true,
        "figures": [
          { "productId": 1, "variantId": 11, "name": "Robe lin", "stock": 0, "soldLast30Days": 6, "daysLeft": 0 },
          { "productId": 2, "variantId": 12, "name": "Chemise", "stock": 3, "soldLast30Days": 9, "daysLeft": 10.0 }
        ] },
      { "id": "first-invoice", "kind": "first_steps", "priority": 40, "title": "Première facture", "text": "Lisez votre première facture.",
        "action": { "screen": "documents.scan", "params": { "type": "facture" } }, "sources": [], "dismissible": true, "figures": [] },
      { "id": "first-campaign", "kind": "first_steps", "priority": 10, "title": "Publicité", "text": "Enregistrez une campagne.",
        "action": { "screen": "campagnes.new", "params": [] }, "sources": [], "dismissible": true, "figures": [] }
    ]
    """;

    [Fact]
    public void Deserialize_ReadsAllTiles_EvenWhenPhpSendsEmptyParamsAsArray()
    {
        var tiles = JsonConvert.DeserializeObject<List<HomeTile>>(Json)!;

        Assert.Equal(3, tiles.Count);
        Assert.Equal("stock.low", tiles[0].Screen);
        Assert.True(tiles[0].IsAlert);
        Assert.Equal("Alerte", tiles[0].KindText);
        Assert.Equal("facture", tiles[1].Param("type"));
        Assert.False(tiles[1].IsAlert);
        Assert.Null(tiles[2].Param("type"));
    }

    [Fact]
    public void SourcesText_JoinsLabelsAndValues_AndIsEmptyWithoutSources()
    {
        var tiles = JsonConvert.DeserializeObject<List<HomeTile>>(Json)!;

        Assert.Equal("Ventes 30 jours : 9 · Stock : 3", tiles[0].SourcesText);
        Assert.True(tiles[0].HasSources);
        Assert.False(tiles[1].HasSources);
    }

    [Fact]
    public void LowStockItems_ReadsFigures_AndDescribesUrgency()
    {
        var tiles = JsonConvert.DeserializeObject<List<HomeTile>>(Json)!;

        var items = tiles[0].LowStockItems();

        Assert.Equal(2, items.Count);
        Assert.Equal("Rupture", items[0].StockText);
        Assert.Equal("À racheter maintenant", items[0].DaysLeftText);
        Assert.Equal("3 en stock", items[1].StockText);
        Assert.Equal("Environ 10 jours de vente", items[1].DaysLeftText);
        Assert.Empty(tiles[1].LowStockItems());
    }

    [Fact]
    public async Task Service_UsesContractUrls()
    {
        var fakeHttp = new FakeHttpService { ResultToReturn = new List<HomeTile>() };
        var service = new HomeTilesService(fakeHttp);

        await service.GetTilesAsync("fr");
        await service.DismissAsync("low-stock");

        Assert.Equal("gestion/accueil/vignettes?locale=fr", Assert.Single(fakeHttp.RequestedUrls));
        Assert.Equal("gestion/accueil/vignettes/low-stock/ecarter", Assert.Single(fakeHttp.PostedAuthData).Url);
    }
}
