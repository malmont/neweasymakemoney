using System.Net;
using System.Text;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Xunit;

namespace Easymakemoney.Tests.Services;

[Collection("SiteCurrency")]
public class HttpServiceTests : IDisposable
{
    public HttpServiceTests()
    {
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        SiteCurrency.Reset();
    }

    public class SampleItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Currency { get; set; }
    }

    [Fact]
    public async Task GetAsync_200Ok_DeserializesObjectSuccessfully()
    {
        var json = """{ "id": 10, "name": "Article Test" }""";
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        var result = await httpService.GetAsync<SampleItem>("items/10");

        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal("Article Test", result.Name);
    }

    [Fact]
    public async Task PostAsyncWithAuth_201Created_DeserializesObjectSuccessfully()
    {
        var json = """{ "id": 99, "name": "Créé" }""";
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        var result = await httpService.PostAsyncWithAuth<SampleItem>("items", new { name = "Créé" });

        Assert.NotNull(result);
        Assert.Equal(99, result.Id);
        Assert.Equal("Créé", result.Name);
    }

    [Fact]
    public async Task PostAsyncWithAuth_204NoContent_ReturnsDefaultWithoutThrowing()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        var result = await httpService.PostAsyncWithAuth<SampleItem?>("items/close", null);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_200WithNullBody_ReturnsNull()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        var result = await httpService.GetAsync<FraisDePort?>("commandes/1/frais-de-port");

        Assert.Null(result);
    }

    [Fact]
    public async Task ListFraisDePortService_404NotFound_ReturnsNullWithoutThrowing()
    {
        var errorJson = """{ "error": "NOT_FOUND", "message": "Aucun frais de port" }""";
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);
        var fraisService = new ListFraisDePortService(httpService);

        var result = await fraisService.GetListFraisDePort(123);

        Assert.Null(result);
    }

    [Fact]
    public async Task CurrencyExtraction_AtRootLevel_UpdatesSiteCurrency()
    {
        var json = """{ "id": 1, "currency": "USD", "name": "Produit US" }""";
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        Assert.Null(SiteCurrency.CurrentCurrency);

        await httpService.GetAsync<SampleItem>("items/1");

        Assert.Equal("USD", SiteCurrency.CurrentCurrency);
    }

    [Fact]
    public async Task CurrencyExtraction_InArrayFirstElement_UpdatesSiteCurrency()
    {
        var json = """[ { "id": 1, "currency": "EUR" }, { "id": 2, "currency": "EUR" } ]""";
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        Assert.Null(SiteCurrency.CurrentCurrency);

        await httpService.GetAsync<List<SampleItem>>("items");

        Assert.Equal("EUR", SiteCurrency.CurrentCurrency);
    }

    [Fact]
    public async Task GetAsync_TaskCanceledException_ThrowsApiExceptionNoConnection()
    {
        var handler = new TestHttpMessageHandler((Func<HttpRequestMessage, HttpResponseMessage>)(_ => throw new TaskCanceledException("Délai dépassé")));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        var ex = await Assert.ThrowsAsync<ApiException>(() => httpService.GetAsync<SampleItem>("items/1"));

        Assert.Equal("NO_CONNECTION", ex.ApiError.Code);
        Assert.Equal("Pas de connexion.", ex.Message);
    }

    [Fact]
    public async Task GetAsync_422ValidationError_ThrowsApiExceptionWithParsedApiError()
    {
        var errorJson = """
        {
            "error": "VALIDATION_FAILED",
            "message": "Données invalides",
            "fields": { "budget": "MUST_BE_INTEGER_CENTS" }
        }
        """;

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        var ex = await Assert.ThrowsAsync<ApiException>(() => httpService.GetAsync<SampleItem>("items/1"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, ex.StatusCode);
        Assert.NotNull(ex.ApiError);
        Assert.Equal("VALIDATION_FAILED", ex.ApiError.Code);
        Assert.Single(ex.ApiError.Errors);
        Assert.Equal("Budget", ex.ApiError.Errors[0].Path);
        Assert.Equal("Montant invalide.", ex.ApiError.Errors[0].Message);
    }
}
