using System.Reflection;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Xunit;

namespace Easymakemoney.Tests.Services;

public class ProductAttachmentServiceTests
{
    [Theory]
    [InlineData("robe & lin #1", "robe%20%26%20lin%20%231")]
    [InlineData("été 2026", "%C3%A9t%C3%A9%202026")]
    [InlineData("simple", "simple")]
    [InlineData(null, "")]
    [InlineData("", "")]
    public async Task GetAvailableAsync_EscapesSearchQueryProperly(string? inputQuery, string expectedEscapedQuery)
    {
        var fakeHttp = new FakeHttpService
        {
            ResultToReturn = new AvailableProductsPage()
        };
        var service = new ProductAttachmentService(fakeHttp);

        await service.GetAvailableAsync(inputQuery, 1, 30);

        Assert.Single(fakeHttp.RequestedUrls);
        var expectedUrl = $"commandes/products/disponibles?q={expectedEscapedQuery}&page=1&limit=30&locale=fr";
        Assert.Equal(expectedUrl, fakeHttp.RequestedUrls[0]);
    }

    [Fact]
    public async Task AttachAsync_OnlyIncludesPurchasePriceWhenEditedCostIsSet()
    {
        var fakeHttp = new FakeHttpService();
        var service = new ProductAttachmentService(fakeHttp);

        var p1 = new AvailableProduct { id = 101, EditedCostCents = 2500L };
        var p2 = new AvailableProduct { id = 102, EditedCostCents = null };

        await service.AttachAsync(42, new[] { p1, p2 });

        Assert.Single(fakeHttp.PostedAuthData);
        var (url, data) = fakeHttp.PostedAuthData[0];
        Assert.Equal("commandes/42/products/rattacher", url);

        // data est un objet anonyme { products = lines }
        var prop = data.GetType().GetProperty("products");
        Assert.NotNull(prop);
        var lines = prop.GetValue(data) as List<Dictionary<string, object>>;
        Assert.NotNull(lines);
        Assert.Equal(2, lines.Count);

        // Premier produit : avec coût
        Assert.Equal(101, lines[0]["productId"]);
        Assert.True(lines[0].ContainsKey("purchasePrice"));
        Assert.Equal(2500L, lines[0]["purchasePrice"]);

        // Deuxième produit : sans coût (la clé purchasePrice ne doit pas être envoyée)
        Assert.Equal(102, lines[1]["productId"]);
        Assert.False(lines[1].ContainsKey("purchasePrice"));
    }

    [Fact]
    public async Task DetachAsync_CallsDeleteRouteWithCommandAndProductIds()
    {
        var fakeHttp = new FakeHttpService();
        var service = new ProductAttachmentService(fakeHttp);

        var result = await service.DetachAsync(15, 99);

        Assert.True(result);
        Assert.Single(fakeHttp.DeletedUrls);
        Assert.Equal("commandes/15/products/99", fakeHttp.DeletedUrls[0]);
    }
}

