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

    [Fact]
    public async Task CurrencyExtraction_SiteCurrencyPrecedesCurrency_UpdatesWithSiteCurrency()
    {
        var json = """{ "id": 1, "siteCurrency": "EUR", "currency": "USD" }""";
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        Assert.Null(SiteCurrency.CurrentCurrency);

        await httpService.GetAsync<SampleItem>("items/1");

        Assert.Equal("EUR", SiteCurrency.CurrentCurrency);
    }

    [Fact]
    public async Task CurrencyExtraction_PurchaseCurrencyPresent_DoesNotChangeSiteCurrency()
    {
        SiteCurrency.SetCurrency("EUR");
        var json = """{ "id": 1, "purchaseCurrency": "USD", "currency": "GBP" }""";
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        await httpService.GetAsync<SampleItem>("items/1");

        Assert.Equal("EUR", SiteCurrency.CurrentCurrency);
    }

    [Fact]
    public async Task PatchAsyncWithAuth_SendsPatchMethodWithJsonBody()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new TestHttpMessageHandler(async req =>
        {
            capturedRequest = req;
            if (req.Content != null)
            {
                capturedBody = await req.Content.ReadAsStringAsync();
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{ "id": 42, "name": "Mis à jour" }""", Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        var result = await httpService.PatchAsyncWithAuth<SampleItem>("periodes/42", new { budget = 150000 });

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Patch, capturedRequest.Method);
        Assert.Contains("150000", capturedBody);
    }

    [Fact]
    public async Task PatchAsyncWithAuth_ErrorResponse_ThrowsApiExceptionWithParsedApiError()
    {
        var errorJson = """
        {
            "error": "PERIOD_CLOSED",
            "message": "Cette période est clôturée"
        }
        """;

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            httpService.PatchAsyncWithAuth<SampleItem>("periodes/42", new { budget = 1000 }));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("PERIOD_CLOSED", ex.ApiError.Code);
    }

    [Fact]
    public async Task PostFilesAsync_Success_SendsMultipartWithFieldsAndFiles()
    {
        HttpRequestMessage? capturedRequest = null;
        var capturedParts = new List<(string? Name, string? FileName, string? ContentType, string Text)>();

        var handler = new TestHttpMessageHandler(async req =>
        {
            capturedRequest = req;
            if (req.Content is MultipartFormDataContent multipart)
            {
                foreach (var part in multipart)
                {
                    var name = part.Headers.ContentDisposition?.Name?.Trim('"');
                    var fileName = part.Headers.ContentDisposition?.FileName?.Trim('"');
                    var contentType = part.Headers.ContentType?.MediaType;
                    var text = await part.ReadAsStringAsync();
                    capturedParts.Add((name, fileName, contentType, text));
                }
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{ "id": 10, "name": "Facture" }""", Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        var fields = new Dictionary<string, string>
        {
            ["type"] = "facture",
            ["locale"] = "fr"
        };

        var fileBytes1 = Encoding.UTF8.GetBytes("CONTENU FACTURE 1");
        var fileBytes2 = Encoding.UTF8.GetBytes("CONTENU FACTURE 2");
        using var stream1 = new MemoryStream(fileBytes1);
        using var stream2 = new MemoryStream(fileBytes2);

        var files = new List<UploadFile>
        {
            new(stream1, "facture1.pdf", "application/pdf"),
            new(stream2, "facture2.jpg", "image/jpeg")
        };

        var result = await httpService.PostFilesAsync<SampleItem>("gestion/documents", fields, "files[]", files);

        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal("Facture", result.Name);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("http://localhost/api/gestion/documents", capturedRequest.RequestUri?.ToString());
        Assert.NotNull(capturedRequest.Content);
        Assert.Equal("multipart/form-data", capturedRequest.Content.Headers.ContentType?.MediaType);

        // Champs texte présents
        Assert.Contains(capturedParts, p => p.Name == "type" && p.Text == "facture");
        Assert.Contains(capturedParts, p => p.Name == "locale" && p.Text == "fr");

        // Fichiers présents
        var fileParts = capturedParts.Where(p => p.Name == "files[]").ToList();
        Assert.Equal(2, fileParts.Count);
        Assert.Equal("facture1.pdf", fileParts[0].FileName);
        Assert.Equal("application/pdf", fileParts[0].ContentType);
        Assert.Equal("CONTENU FACTURE 1", fileParts[0].Text);

        Assert.Equal("facture2.jpg", fileParts[1].FileName);
        Assert.Equal("image/jpeg", fileParts[1].ContentType);
        Assert.Equal("CONTENU FACTURE 2", fileParts[1].Text);
    }

    [Fact]
    public async Task PostFilesAsync_422ValidationError_ThrowsApiException()
    {
        var errorJson = """
        {
            "error": "INVALID_FORMAT",
            "message": "Type de fichier non supporté"
        }
        """;

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var files = new List<UploadFile> { new(stream, "test.bin", "application/octet-stream") };

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            httpService.PostFilesAsync<SampleItem>("gestion/documents", new Dictionary<string, string>(), "files[]", files));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, ex.StatusCode);
        Assert.Equal("INVALID_FORMAT", ex.ApiError.Code);
    }

    [Fact]
    public async Task PostFilesAsync_402PaymentRequired_ThrowsApiException()
    {
        var errorJson = """
        {
            "error": "QUOTA_EXCEEDED",
            "message": "Quota dépassé"
        }
        """;

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.PaymentRequired)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var files = new List<UploadFile> { new(stream, "test.pdf", "application/pdf") };

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            httpService.PostFilesAsync<SampleItem>("gestion/documents", new Dictionary<string, string>(), "files[]", files));

        Assert.Equal(HttpStatusCode.PaymentRequired, ex.StatusCode);
        Assert.Equal("QUOTA_EXCEEDED", ex.ApiError.Code);
    }

    [Fact]
    public async Task PostFilesAsync_NetworkFailure_ThrowsApiExceptionWithNoConnection()
    {
        var handler = new TestHttpMessageHandler((Func<HttpRequestMessage, HttpResponseMessage>)(_ =>
            throw new HttpRequestException("Connexion impossible")));

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var files = new List<UploadFile> { new(stream, "test.pdf", "application/pdf") };

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            httpService.PostFilesAsync<SampleItem>("gestion/documents", new Dictionary<string, string>(), "files[]", files));

        Assert.Equal("NO_CONNECTION", ex.ApiError.Code);
    }

    [Fact]
    public async Task PostFilesAsync_Timeout_ThrowsApiExceptionWithNoConnection()
    {
        var handler = new TestHttpMessageHandler((Func<HttpRequestMessage, HttpResponseMessage>)(_ =>
            throw new TaskCanceledException("Timeout")));

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var files = new List<UploadFile> { new(stream, "test.pdf", "application/pdf") };

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            httpService.PostFilesAsync<SampleItem>("gestion/documents", new Dictionary<string, string>(), "files[]", files));

        Assert.Equal("NO_CONNECTION", ex.ApiError.Code);
    }

    [Fact]
    public async Task PutAsyncWithAuth_SendsPutMethodWithJsonBodyAndReturnsDeserializedObject()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new TestHttpMessageHandler(async req =>
        {
            capturedRequest = req;
            if (req.Content != null)
            {
                capturedBody = await req.Content.ReadAsStringAsync();
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{ "id": 55, "name": "Mis à jour en PUT" }""", Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        var result = await httpService.PutAsyncWithAuth<SampleItem>("items/55", new { name = "Mis à jour en PUT" });

        Assert.NotNull(result);
        Assert.Equal(55, result.Id);
        Assert.Equal("Mis à jour en PUT", result.Name);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Put, capturedRequest.Method);
        Assert.Equal("application/json", capturedRequest.Content?.Headers.ContentType?.MediaType);
        Assert.Contains("Mis à jour en PUT", capturedBody);
    }

    [Fact]
    public async Task PutAsyncWithAuth_409Conflict_ThrowsApiExceptionWithParsedCode()
    {
        var errorJson = """{ "error": "PRICE_EDITED_SINCE", "message": "Conflit d'édition" }""";
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            httpService.PutAsyncWithAuth<SampleItem>("items/55", new { name = "test" }));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("PRICE_EDITED_SINCE", ex.ApiError.Code);
    }

    [Fact]
    public async Task PutAsyncWithAuth_422ValidationError_ThrowsApiExceptionWithParsedFields()
    {
        var errorJson = """
        {
            "error": "VALIDATION_FAILED",
            "message": "Erreur de validation",
            "fields": { "threshold": "MUST_BE_INTEGER_CENTS" }
        }
        """;
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            httpService.PutAsyncWithAuth<SampleItem>("items/55", new { threshold = "invalid" }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, ex.StatusCode);
        Assert.Equal("VALIDATION_FAILED", ex.ApiError.Code);
        Assert.Single(ex.ApiError.Errors);
    }

    [Fact]
    public async Task PutAsyncWithAuth_NetworkFailure_ThrowsApiExceptionWithNoConnection()
    {
        var handler = new TestHttpMessageHandler((Func<HttpRequestMessage, HttpResponseMessage>)(_ =>
            throw new HttpRequestException("Connexion impossible")));

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            httpService.PutAsyncWithAuth<SampleItem>("items/55", new { name = "test" }));

        Assert.Equal("NO_CONNECTION", ex.ApiError.Code);
    }

    [Fact]
    public async Task PutAsyncWithAuth_Timeout_ThrowsApiExceptionWithNoConnection()
    {
        var handler = new TestHttpMessageHandler((Func<HttpRequestMessage, HttpResponseMessage>)(_ =>
            throw new TaskCanceledException("Délai dépassé")));

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            httpService.PutAsyncWithAuth<SampleItem>("items/55", new { name = "test" }));

        Assert.Equal("NO_CONNECTION", ex.ApiError.Code);
    }

    [Theory]
    [InlineData("PRICES_CHANGED", HttpStatusCode.Conflict)]
    [InlineData("PROMOTION_EDITED_SINCE", HttpStatusCode.Conflict)]
    [InlineData("PRICE_EDITED_SINCE", HttpStatusCode.Conflict)]
    [InlineData("COMMENT_LIMIT", (HttpStatusCode)429)]
    [InlineData("TRIP_OVERLAP", HttpStatusCode.Conflict)]
    public async Task BusinessErrors_ExpectedByScreens_PropagateExactCodeAndStatusCode(string errorCode, HttpStatusCode statusCode)
    {
        var errorJson = $"{{\"error\":\"{errorCode}\"}}";
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            httpService.PostAsyncWithAuth<SampleItem>("test/endpoint", new { }));

        Assert.Equal(statusCode, ex.StatusCode);
        Assert.Equal(errorCode, ex.ApiError.Code);
    }
}
