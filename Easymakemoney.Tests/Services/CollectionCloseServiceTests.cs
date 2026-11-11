using System.Net;
using System.Text;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Xunit;

namespace Easymakemoney.Tests.Services;

public class CollectionCloseServiceTests
{
    [Fact]
    public async Task CloseCollectionAsync_Success_CallsCorrectUrlWithNullBodyAndReturnsTrue()
    {
        var fakeHttp = new FakeHttpService();
        var service = new CollectionCloseService(fakeHttp);

        var result = await service.CloseCollectionAsync(10);

        Assert.True(result);
        Assert.Single(fakeHttp.PostedAuthData);
        Assert.Equal("dashboard/collection/10/close", fakeHttp.PostedAuthData[0].Url);
        Assert.Null(fakeHttp.PostedAuthData[0].Data);
    }

    [Fact]
    public async Task ReopenCollectionAsync_Success_CallsCorrectUrlWithNullBodyAndReturnsTrue()
    {
        var fakeHttp = new FakeHttpService();
        var service = new CollectionCloseService(fakeHttp);

        var result = await service.ReopenCollectionAsync(10);

        Assert.True(result);
        Assert.Single(fakeHttp.PostedAuthData);
        Assert.Equal("dashboard/collection/10/reopen", fakeHttp.PostedAuthData[0].Url);
        Assert.Null(fakeHttp.PostedAuthData[0].Data);
    }

    [Fact]
    public async Task CloseCollectionAsync_409PeriodClosed_ThrowsApiExceptionWithCorrectCode()
    {
        var errorJson = """{ "error": "PERIOD_CLOSED", "message": "Cette période est clôturée" }""";
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);
        var service = new CollectionCloseService(httpService);

        var ex = await Assert.ThrowsAsync<ApiException>(() => service.CloseCollectionAsync(10));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("PERIOD_CLOSED", ex.ApiError.Code);
    }

    [Fact]
    public async Task ReopenCollectionAsync_409NotClosed_ThrowsApiExceptionWithCorrectCode()
    {
        var errorJson = """{ "error": "NOT_CLOSED", "message": "Cette période est déjà ouverte" }""";
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);
        var service = new CollectionCloseService(httpService);

        var ex = await Assert.ThrowsAsync<ApiException>(() => service.ReopenCollectionAsync(10));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("NOT_CLOSED", ex.ApiError.Code);
    }
}

