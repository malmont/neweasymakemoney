using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Xunit;

namespace Easymakemoney.Tests.Services;

public class ListCommandServiceTests
{
    private static ListCommand MakeCommand() => new()
    {
        name = "Commande Test",
        date = "2026-10-15T00:00:00Z",
        photo = "photo.png",
        collectionId = 0,
        fournisseur = new ListFournisseur { name = "Fournisseur A" }
    };

    [Fact]
    public async Task PostCommandes_WhenIdIsZero_CallsGeneralCommandesRoute()
    {
        var fakeHttp = new FakeHttpService();
        var service = new ListCommandService(fakeHttp);

        var cmd = MakeCommand();
        var result = await service.PostCommandes(cmd, 0);

        Assert.True(result);
        Assert.Single(fakeHttp.PostedAuthData);
        var (url, data) = fakeHttp.PostedAuthData[0];
        Assert.Equal("commandes", url);
        Assert.Same(cmd, data);
    }

    [Fact]
    public async Task PostCommandes_WhenIdIsGreaterThanZero_CallsCollectionCommandesRoute()
    {
        var fakeHttp = new FakeHttpService();
        var service = new ListCommandService(fakeHttp);

        var cmd = MakeCommand();
        var result = await service.PostCommandes(cmd, 15);

        Assert.True(result);
        Assert.Single(fakeHttp.PostedAuthData);
        var (url, data) = fakeHttp.PostedAuthData[0];
        Assert.Equal("collections/15/commandes", url);
        Assert.Same(cmd, data);
    }
}

