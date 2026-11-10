using System.Net;
using Easymakemoney.Models.Tenant;
using Easymakemoney.Services.Tenant;
using Easymakemoney.Tests.Fakes;
using Xunit;

namespace Easymakemoney.Tests.Services;

public class TenantHandlerTests
{
    [Fact]
    public async Task SendAsync_ConfiguredTenant_AddsXTenantHostHeader()
    {
        var tenantStore = new FakeTenantStore
        {
            CurrentTenant = new TenantContext
            {
                Host = "demo.exemple.test",
                Code = "DEMO",
                Name = "Site Demo"
            }
        };

        var innerHandler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var tenantHandler = new TenantHandler(tenantStore) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(tenantHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/items");
        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(innerHandler.SentRequests);
        var sentReq = innerHandler.SentRequests[0];
        Assert.True(sentReq.Headers.Contains("X-Tenant-Host"));
        Assert.Equal("demo.exemple.test", sentReq.Headers.GetValues("X-Tenant-Host").Single());
    }

    [Fact]
    public async Task SendAsync_NoTenantConfigured_ThrowsTenantMissingException()
    {
        var tenantStore = new FakeTenantStore { CurrentTenant = null };
        var innerHandler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var tenantHandler = new TenantHandler(tenantStore) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(tenantHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/items");

        await Assert.ThrowsAsync<TenantMissingException>(() => invoker.SendAsync(request, CancellationToken.None));
        Assert.Empty(innerHandler.SentRequests);
    }

    [Fact]
    public async Task SendAsync_TenantWithEmptyHost_ThrowsTenantMissingException()
    {
        var tenantStore = new FakeTenantStore
        {
            CurrentTenant = new TenantContext { Host = "   ", Code = "DEMO" }
        };
        var innerHandler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var tenantHandler = new TenantHandler(tenantStore) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(tenantHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/items");

        await Assert.ThrowsAsync<TenantMissingException>(() => invoker.SendAsync(request, CancellationToken.None));
        Assert.Empty(innerHandler.SentRequests);
    }

    [Fact]
    public async Task SendAsync_SkipTenantHost_DoesNotAddXTenantHost()
    {
        var tenantStore = new FakeTenantStore { CurrentTenant = null }; // Aucun tenant configuré
        var innerHandler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var tenantHandler = new TenantHandler(tenantStore) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(tenantHandler);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/app/activate");
        request.SetSkipTenantHost(true);

        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(innerHandler.SentRequests);
        Assert.False(innerHandler.SentRequests[0].Headers.Contains("X-Tenant-Host"));
    }

    [Fact]
    public async Task SendAsync_CustomTenantHost_UsesCustomHost()
    {
        var tenantStore = new FakeTenantStore { CurrentTenant = null };
        var innerHandler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var tenantHandler = new TenantHandler(tenantStore) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(tenantHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/tenant/check-mobile");
        request.SetCustomTenantHost("custom.exemple.test");

        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(innerHandler.SentRequests);
        var sentReq = innerHandler.SentRequests[0];
        Assert.True(sentReq.Headers.Contains("X-Tenant-Host"));
        Assert.Equal("custom.exemple.test", sentReq.Headers.GetValues("X-Tenant-Host").Single());
    }

    [Fact]
    public async Task SendAsync_CustomTenantHostEmpty_ThrowsInvalidOperationException()
    {
        var tenantStore = new FakeTenantStore { CurrentTenant = null };
        var innerHandler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var tenantHandler = new TenantHandler(tenantStore) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(tenantHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/tenant/check-mobile");
        request.SetCustomTenantHost("   ");

        await Assert.ThrowsAsync<InvalidOperationException>(() => invoker.SendAsync(request, CancellationToken.None));
        Assert.Empty(innerHandler.SentRequests);
    }

    [Fact]
    public async Task SendAsync_SpoofedHeaderPresent_OverwritesWithLegitimateTenant()
    {
        var tenantStore = new FakeTenantStore
        {
            CurrentTenant = new TenantContext { Host = "legitimate.exemple.test", Code = "LEGIT" }
        };
        var innerHandler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var tenantHandler = new TenantHandler(tenantStore) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(tenantHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/items");
        request.Headers.Add("X-Tenant-Host", "spoofed.hacker.test");

        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sentReq = innerHandler.SentRequests[0];
        var values = sentReq.Headers.GetValues("X-Tenant-Host").ToList();
        Assert.Single(values);
        Assert.Equal("legitimate.exemple.test", values[0]);
    }
}

