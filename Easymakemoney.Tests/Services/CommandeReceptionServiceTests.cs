using System.Net;
using System.Text;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.Services;

public class CommandeReceptionServiceTests
{
    [Fact]
    public async Task ReceiveAsync_WithNullBody_CallsCorrectUrlAndParsesCompleteReception()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var jsonResponse = """
        {
            "received": [
                { "lineId": 1, "quantity": 3 },
                { "lineId": 2, "quantity": 2 }
            ],
            "commande": {
                "id": 12,
                "remaining": 0,
                "receptionStatus": "complete"
            }
        }
        """;

        var handler = new TestHttpMessageHandler(async req =>
        {
            capturedRequest = req;
            if (req.Content != null)
            {
                capturedBody = await req.Content.ReadAsStringAsync();
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);
        var service = new CommandeReceptionService(httpService);

        var result = await service.ReceiveAsync(12, null);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("http://localhost/api/commandes/12/reception", capturedRequest.RequestUri?.ToString());
        Assert.True(string.IsNullOrEmpty(capturedBody) || capturedBody == "null");

        Assert.NotNull(result);
        Assert.Equal(2, result.ReceivedLines);
        Assert.Equal(5, result.ReceivedQuantity);
        Assert.Equal(12, result.CommandeId);
        Assert.Equal(0, result.Remaining);
        Assert.Equal("complete", result.ReceptionStatus);
        Assert.Empty(result.NeedsVariant);
    }

    [Fact]
    public async Task ReceiveAsync_WithBody_TransmitsBodyAsIs_AndParsesNeedsVariant()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var jsonResponse = """
        {
            "received": [],
            "needsVariant": [
                {
                    "lineId": 4,
                    "product": { "id": 99, "name": "Robe lin" },
                    "variants": [
                        { "id": 201, "size": "M", "color": "Rouge" },
                        { "id": 202, "size": "L", "color": "Bleu" }
                    ]
                }
            ],
            "commande": {
                "id": 12,
                "remaining": 2,
                "receptionStatus": "partial"
            }
        }
        """;

        var handler = new TestHttpMessageHandler(async req =>
        {
            capturedRequest = req;
            if (req.Content != null)
            {
                capturedBody = await req.Content.ReadAsStringAsync();
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);
        var service = new CommandeReceptionService(httpService);

        var choicesBody = CommandeReception.VariantChoicesBody(new[] { (LineId: 4, VariantId: 201) });
        var result = await service.ReceiveAsync(12, choicesBody);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("http://localhost/api/commandes/12/reception", capturedRequest.RequestUri?.ToString());
        Assert.NotNull(capturedBody);
        var sentJson = JObject.Parse(capturedBody);
        var sentLines = (JArray)sentJson["lines"]!;
        Assert.Single(sentLines);
        Assert.Equal(4, sentLines[0]["lineId"]!.Value<int>());
        Assert.Equal(201, sentLines[0]["variantId"]!.Value<int>());

        Assert.NotNull(result);
        Assert.Equal(0, result.ReceivedQuantity);
        Assert.Equal(2, result.Remaining);
        Assert.Equal("partial", result.ReceptionStatus);
        Assert.Single(result.NeedsVariant);

        var need = result.NeedsVariant[0];
        Assert.Equal(4, need.LineId);
        Assert.Equal(99, need.ProductId);
        Assert.Equal("Robe lin", need.ProductName);
        Assert.Equal(2, need.Variants.Count);

        var labels = need.ChoiceLabels();
        Assert.Equal(2, labels.Length);
        Assert.Contains("Taille M · Couleur Rouge", labels[0]);
        Assert.Contains("Taille L · Couleur Bleu", labels[1]);
    }
}
