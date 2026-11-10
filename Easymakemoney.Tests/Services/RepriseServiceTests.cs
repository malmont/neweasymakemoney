using System.Collections;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Xunit;

namespace Easymakemoney.Tests.Services;

public class RepriseServiceTests
{
    [Fact]
    public async Task SimulateAsync_WithCoefficient_SendsCoefficientInBody()
    {
        var fakeHttp = new FakeHttpService
        {
            ResultToReturn = new RepriseSimulation()
        };
        var service = new RepriseService(fakeHttp);

        await service.SimulateAsync(12, 2.5, 1, 20);

        Assert.Single(fakeHttp.PostedAuthData);
        var (url, bodyObj) = fakeHttp.PostedAuthData[0];
        Assert.Equal("gestion/reprise/simulation", url);

        var dict = Assert.IsType<Dictionary<string, object>>(bodyObj);
        Assert.Equal(12, dict["months"]);
        Assert.Equal(2.5, dict["estimateCoefficient"]);
        Assert.Equal(1, dict["page"]);
        Assert.Equal(20, dict["limit"]);
    }

    [Fact]
    public async Task SimulateAsync_WithoutCoefficient_OmitsEstimateCoefficientKey()
    {
        var fakeHttp = new FakeHttpService
        {
            ResultToReturn = new RepriseSimulation()
        };
        var service = new RepriseService(fakeHttp);

        await service.SimulateAsync(6, null, 2, 50);

        Assert.Single(fakeHttp.PostedAuthData);
        var (url, bodyObj) = fakeHttp.PostedAuthData[0];
        Assert.Equal("gestion/reprise/simulation", url);

        var dict = Assert.IsType<Dictionary<string, object>>(bodyObj);
        Assert.Equal(6, dict["months"]);
        Assert.False(dict.ContainsKey("estimateCoefficient"));
        Assert.Equal(2, dict["page"]);
        Assert.Equal(50, dict["limit"]);
    }

    [Fact]
    public async Task ApplyAsync_WithEditedCosts_BuildsCorrectPayload()
    {
        var fakeHttp = new FakeHttpService();
        var service = new RepriseService(fakeHttp);

        var product = new RepriseProduct
        {
            productId = 10,
            EditedCostCents = 3500L
        };

        await service.ApplyAsync(12, 2.0, new[] { product });

        Assert.Single(fakeHttp.PostedAuthData);
        var (url, bodyObj) = fakeHttp.PostedAuthData[0];
        Assert.Equal("gestion/reprise/appliquer", url);

        var dict = Assert.IsType<Dictionary<string, object>>(bodyObj);
        Assert.Equal(12, dict["months"]);
        Assert.Equal(2.0, dict["estimateCoefficient"]);

        var costsList = Assert.IsAssignableFrom<IEnumerable>(dict["costs"]);
        var costItems = costsList.Cast<object>().ToList();
        Assert.Single(costItems);

        var firstCost = costItems[0];
        var pIdProp = firstCost.GetType().GetProperty("productId");
        var pPriceProp = firstCost.GetType().GetProperty("purchasePrice");

        Assert.NotNull(pIdProp);
        Assert.NotNull(pPriceProp);
        Assert.Equal(10, pIdProp.GetValue(firstCost));
        Assert.Equal(3500L, pPriceProp.GetValue(firstCost));
    }

    [Fact]
    public async Task ApplyAsync_WithoutCoefficient_OmitsEstimateCoefficientKey()
    {
        var fakeHttp = new FakeHttpService();
        var service = new RepriseService(fakeHttp);

        await service.ApplyAsync(12, null, Array.Empty<RepriseProduct>());

        Assert.Single(fakeHttp.PostedAuthData);
        var (url, bodyObj) = fakeHttp.PostedAuthData[0];
        Assert.Equal("gestion/reprise/appliquer", url);

        var dict = Assert.IsType<Dictionary<string, object>>(bodyObj);
        Assert.Equal(12, dict["months"]);
        Assert.False(dict.ContainsKey("estimateCoefficient"));
    }

    [Fact]
    public async Task CancelAsync_SendsPostWithNullBodyToAnnulerRoute()
    {
        var fakeHttp = new FakeHttpService();
        var service = new RepriseService(fakeHttp);

        await service.CancelAsync();

        Assert.Single(fakeHttp.PostedAuthData);
        var (url, body) = fakeHttp.PostedAuthData[0];
        Assert.Equal("gestion/reprise/annuler", url);
        Assert.Null(body);
    }

    [Fact]
    public async Task GetStateAsync_CallsGestionRepriseRoute()
    {
        var fakeHttp = new FakeHttpService
        {
            ResultToReturn = new RepriseState()
        };
        var service = new RepriseService(fakeHttp);

        await service.GetStateAsync();

        Assert.Single(fakeHttp.RequestedUrls);
        Assert.Equal("gestion/reprise", fakeHttp.RequestedUrls[0]);
    }
}

