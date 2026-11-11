using System.Globalization;
using System.Net;
using System.Text;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Xunit;

namespace Easymakemoney.Tests.Services;

[Collection("SiteCurrency")]
public class PeriodResultServiceTests : IDisposable
{
    private readonly CultureInfo _originalCulture;

    public PeriodResultServiceTests()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        SiteCurrency.Reset();
        CultureInfo.CurrentCulture = _originalCulture;
    }

    [Fact]
    public async Task GetResultAsync_CallsCorrectUrl_AndReturnsDeserializedPeriodResultWithDisplayTexts()
    {
        SiteCurrency.SetCurrency("EUR");
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        var json = """
        {
            "frozen": false,
            "sales": { "revenue": 150000, "unknownCostRevenue": 20000 },
            "otherRevenue": { "total": 10000 },
            "cogs": { "amount": 80000, "unknownCostItems": 2, "estimatedItems": 1, "estimatedAmount": 15000 },
            "expenses": { "shipping": 5000, "notes": 3000, "other": [] },
            "result": { "gross": 50000, "net": 42000, "netRate": 32.3 },
            "investment": { "purchases": 90000, "shipping": 5000, "total": 95000 }
        }
        """;

        HttpRequestMessage? capturedRequest = null;
        var handler = new TestHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var httpService = new HttpService(client);
        var service = new PeriodResultService(httpService);

        var pr = await service.GetResultAsync(42);

        // 1. Vérification de l'URL appelée (D2)
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal("http://localhost/api/periodes/42/resultat", capturedRequest.RequestUri?.ToString());

        // 2. Vérification des textes d'affichage non couverts auparavant (D2)
        Assert.NotNull(pr);

        // NetText (420 €)
        Assert.Contains("420", pr.NetText);
        Assert.Contains("€", pr.NetText);

        // GrossText (500 €)
        Assert.Contains("500", pr.GrossText);
        Assert.Contains("€", pr.GrossText);

        // RevenueText (1500 €)
        Assert.Contains("1", pr.RevenueText);
        Assert.Contains("500", pr.RevenueText);
        Assert.Contains("€", pr.RevenueText);

        // UnknownRevenueText (− 200 €)
        Assert.StartsWith("−", pr.UnknownRevenueText);
        Assert.Contains("200", pr.UnknownRevenueText);
        Assert.Contains("€", pr.UnknownRevenueText);

        // CogsText (− 800 €)
        Assert.StartsWith("−", pr.CogsText);
        Assert.Contains("800", pr.CogsText);
        Assert.Contains("€", pr.CogsText);

        // ShippingText (− 50 €)
        Assert.StartsWith("−", pr.ShippingText);
        Assert.Contains("50", pr.ShippingText);
        Assert.Contains("€", pr.ShippingText);

        // NotesText (− 30 €)
        Assert.StartsWith("−", pr.NotesText);
        Assert.Contains("30", pr.NotesText);
        Assert.Contains("€", pr.NotesText);

        // OtherRevenueText (100 €)
        Assert.Contains("100", pr.OtherRevenueText);
        Assert.Contains("€", pr.OtherRevenueText);

        // UnknownCostDetail (contient le montant des ventes hors marge et le texte explicatif)
        Assert.Contains("200", pr.UnknownCostDetail);
        Assert.Contains("ventes hors marge", pr.UnknownCostDetail);
    }
}

