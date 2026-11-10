using System.Net;
using System.Text;
using Easymakemoney.Services.Site;
using Easymakemoney.Tests.Fakes;
using Xunit;

namespace Easymakemoney.Tests.Services;

public class ActivationTests
{
    [Theory]
    [InlineData("IZ-ADM-ABCD-1234-EF56", "IZADMABCD1234EF56")]
    [InlineData("  iz-ges-abcd-1234-ef56  ", "IZGESABCD1234EF56")]
    [InlineData("izadm0123456789ab", "IZADM0123456789AB")]
    public void ValidateAppKeyFormat_ValidKeyFormats_ReturnsTrueAndCleanedKey(string input, string expectedCleaned)
    {
        var isValid = SiteService.ValidateAppKeyFormat(input, out var cleaned);

        Assert.True(isValid);
        Assert.Equal(expectedCleaned, cleaned);
    }

    [Theory]
    [InlineData("TROPCOURT")]
    [InlineData("IZ-ADM-TROP-LONG-1234567890")]
    [InlineData("XX-ADM-ABCD-1234-EF56")] // Ne commence pas par IZ
    [InlineData("IZ-123-ABCD-1234-EF56")] // Pas 3 lettres pour l'app
    [InlineData("IZ-ADM-ABCD-1234-EF5!")] // Caractère spécial
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAppKeyFormat_InvalidFormats_ReturnsFalseAndEmptyKey(string input)
    {
        var isValid = SiteService.ValidateAppKeyFormat(input, out var cleaned);

        Assert.False(isValid);
        Assert.Equal(string.Empty, cleaned);
    }

    [Fact]
    public void ValidateAppKeyFormat_NullInput_ReturnsFalseAndEmptyKey()
    {
        var isValid = SiteService.ValidateAppKeyFormat(null, out var cleaned);

        Assert.False(isValid);
        Assert.Equal(string.Empty, cleaned);
    }

    [Theory]
    [InlineData("https://www.exemple.test", "exemple.test")]
    [InlineData("http://shop.exemple.test/path/to/resource", "shop.exemple.test")]
    [InlineData("  WWW.BOUTIQUE.EXEMPLE.TEST:8080/api  ", "boutique.exemple.test")]
    [InlineData("  mon-site.exemple.test  ", "mon-site.exemple.test")]
    [InlineData("ABC-DEF", "abc-def")]
    public void CleanSiteInput_NormalizesDomainOrCode(string input, string expected)
    {
        var result = SiteService.CleanSiteInput(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CleanSiteInput_NullOrWhitespace_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, SiteService.CleanSiteInput(null));
        Assert.Equal(string.Empty, SiteService.CleanSiteInput(""));
        Assert.Equal(string.Empty, SiteService.CleanSiteInput("   "));
    }

    [Fact]
    public async Task ActivateWithKeyAsync_ValidResponse_ReturnsSuccessWithTenantContext()
    {
        var jsonResponse = """
        {
            "tenant": {
                "code": "TST001",
                "name": "Boutique Fictive",
                "host": "fictive.exemple.test"
            },
            "app": "admin",
            "label": "Appareil Test",
            "expiresAt": "2027-12-31T23:59:59Z"
        }
        """;

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var siteService = new SiteService(httpClient, new FakeDeviceInfoService { DeviceName = "Appareil Test" });

        var result = await siteService.ActivateWithKeyAsync("IZ-ADM-ABCD-1234-EF56");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Tenant);
        Assert.Equal("TST001", result.Tenant.Code);
        Assert.Equal("Boutique Fictive", result.Tenant.Name);
        Assert.Equal("fictive.exemple.test", result.Tenant.Host);
        Assert.Equal("Appareil Test", result.Tenant.Label);
    }

    [Fact]
    public async Task ActivateWithKeyAsync_404AppKeyInvalid_ReturnsFailureWithApiError()
    {
        var jsonResponse = """
        {
            "error": "APP_KEY_INVALID",
            "code": "APP_KEY_INVALID",
            "message": "Cette clé d'activation est invalide, inconnue ou expirée."
        }
        """;

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var siteService = new SiteService(httpClient, new FakeDeviceInfoService());

        var result = await siteService.ActivateWithKeyAsync("IZ-ADM-ABCD-1234-EF56");

        Assert.False(result.IsSuccess);
        Assert.Equal("Cette clé d'activation est invalide, inconnue ou expirée.", result.ErrorMessage);
        Assert.NotNull(result.ApiError);
        Assert.Equal("APP_KEY_INVALID", result.ApiError.Code);
    }

    [Fact]
    public async Task ActivateWithKeyAsync_InvalidFormat_FailsLocallyWithoutHttpCall()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var siteService = new SiteService(httpClient, new FakeDeviceInfoService());

        var result = await siteService.ActivateWithKeyAsync("CLE_INVALIDE");

        Assert.False(result.IsSuccess);
        Assert.Contains("format de la clé d'activation est invalide", result.ErrorMessage);
        Assert.Empty(handler.SentRequests);
    }

    [Fact]
    public async Task CheckSiteCodeAsync_EmptyInput_FailsLocallyWithoutHttpCall()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var siteService = new SiteService(httpClient, new FakeDeviceInfoService());

        var result = await siteService.CheckSiteCodeAsync("   ");

        Assert.False(result.IsSuccess);
        Assert.Equal("Saisissez le code du site.", result.ErrorMessage);
        Assert.Empty(handler.SentRequests);
    }

    [Fact]
    public async Task CheckSiteCodeAsync_ValidResponse_ReturnsSuccessWithTenantContext()
    {
        var jsonResponse = """
        {
            "exists": true,
            "tenant": {
                "code": "TST002",
                "name": "Boutique Deux",
                "host": "deux.exemple.test"
            }
        }
        """;

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var siteService = new SiteService(httpClient, new FakeDeviceInfoService());

        var result = await siteService.CheckSiteCodeAsync("deux.exemple.test");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Tenant);
        Assert.Equal("TST002", result.Tenant.Code);
        Assert.Equal("deux.exemple.test", result.Tenant.Host);
        Assert.Equal("Boutique Deux", result.Tenant.Name);
    }

    [Fact]
    public async Task CheckSiteCodeAsync_404NotFound_ReturnsFailureWithApiError()
    {
        var jsonResponse = """
        {
            "error": "TENANT_NOT_FOUND",
            "code": "TENANT_NOT_FOUND",
            "message": "Site introuvable."
        }
        """;

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var siteService = new SiteService(httpClient, new FakeDeviceInfoService());

        var result = await siteService.CheckSiteCodeAsync("inconnu.exemple.test");

        Assert.False(result.IsSuccess);
        Assert.Equal("Site introuvable.", result.ErrorMessage);
        Assert.NotNull(result.ApiError);
        Assert.Equal("TENANT_NOT_FOUND", result.ApiError.Code);
    }
}
