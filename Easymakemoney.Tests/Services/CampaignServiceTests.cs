using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.Services;

public class CampaignServiceTests
{
    [Fact]
    public async Task ListAsync_AsksOnePage()
    {
        var http = new FakeHttpService { ResultToReturn = new List<Campaign>() };
        var service = new CampaignService(http);

        var result = await service.ListAsync(2, 50);

        Assert.Empty(result);
        Assert.Equal("gestion/campagnes?page=2&limit=50", Assert.Single(http.RequestedUrls));
    }

    [Fact]
    public async Task Routes_FollowTheContract()
    {
        var http = new FakeHttpService();
        var service = new CampaignService(http);
        var body = new JObject { ["name"] = "Hiver" };

        await service.GetAsync(7);
        await service.GetProfitabilityAsync(7);
        await service.CreateAsync(body);
        await service.UpdateAsync(7, body);
        await service.DeleteAsync(7);

        Assert.Equal(new[] { "gestion/campagnes/7", "gestion/campagnes/7/rentabilite" }, http.RequestedUrls);
        Assert.Equal(new[] { "gestion/campagnes", "gestion/campagnes/7" }, http.PostedAuthData.Select(p => p.Url));
        Assert.Same(body, http.PostedAuthData[0].Data);
        Assert.Equal("gestion/campagnes/7", Assert.Single(http.DeletedUrls));
    }
}
