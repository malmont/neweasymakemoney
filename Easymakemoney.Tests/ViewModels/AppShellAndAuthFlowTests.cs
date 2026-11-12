using System.Net;
using Easymakemoney.Models.Session;
using Easymakemoney.Models.Tenant;
using Easymakemoney.Services.Auth;
using Easymakemoney.Services.Offline;
using Easymakemoney.Services.Tenant;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public sealed class AppShellAndAuthFlowTests : IDisposable
{
    public void Dispose()
    {
        OfflineState.Online();
        SiteCurrency.Reset();
    }

    private sealed class Harness
    {
        public FakeTenantStore TenantStore { get; } = new()
        {
            CurrentTenant = new TenantContext { Code = "DEM042", Host = "demo.example", Name = "Démo" }
        };

        public FakeSessionService Session { get; } = new()
        {
            Token = "jwt_actif",
            RefreshToken = "rt_actif",
            CurrentUser = new UserInfo { Id = 42, Email = "admin@example.test" },
            LastLoginEmail = "admin@example.test"
        };

        public MemoryResponseCache Cache { get; } = new();
        public PendingLoginStore PendingStore { get; } = new();
        public TestHttpMessageHandler HttpHandler { get; }
        public SiteDataCleaner Cleaner { get; }
        public AuthFlowService AuthFlow { get; }
        public AppShellViewModel ShellVm { get; }
        public List<string> Routes { get; } = new();
        public int DeviceStorageClears { get; private set; }
        public bool ConfirmChoice { get; set; } = true;

        public Harness()
        {
            Cache.Save("demo.example:42:gestion/statistiques", "{\"total\":100}", DateTime.UtcNow);
            Cache.Save("demo.example:42:gestion/tresorerie", "{\"solde\":500}", DateTime.UtcNow);
            PendingStore.Set("admin@example.test", "secret");

            HttpHandler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var client = new HttpClient(HttpHandler) { BaseAddress = new Uri("https://demo.example/api/") };

            Cleaner = new SiteDataCleaner(TenantStore, Session, Cache)
            {
                ClearDeviceStorage = () => DeviceStorageClears++
            };

            AuthFlow = new AuthFlowService(
                new FakeHttpClientFactory(client),
                Session,
                Cleaner,
                PendingStore)
            {
                RunOnMainThreadAsync = action => action(),
                ConfirmAsync = (_, _, _, _) => Task.FromResult(ConfirmChoice),
                NavigateAsync = route =>
                {
                    Routes.Add(route);
                    return Task.CompletedTask;
                }
            };

            ShellVm = new AppShellViewModel(AuthFlow);
        }
    }

    [Fact]
    public async Task SignOut_RevokesToken_ClearsSession_KeepsTenantLastEmailAndOfflineCache()
    {
        var h = new Harness();

        await h.ShellVm.SignOutCommand.ExecuteAsync(null);

        Assert.Single(h.HttpHandler.SentRequests);
        Assert.EndsWith("/logout", h.HttpHandler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(1, h.Session.ClearSessionCalls);
        Assert.Null(h.Session.Token);
        Assert.Null(h.Session.RefreshToken);
        Assert.Null(h.Session.CurrentUser);
        Assert.False(h.PendingStore.TryGet(out _, out _));

        // La déconnexion garde le site, le courriel mémorisé, les préférences et le cache hors connexion
        Assert.NotNull(h.TenantStore.CurrentTenant);
        Assert.Equal("DEM042", h.TenantStore.CurrentTenant.Code);
        Assert.Equal("admin@example.test", h.Session.LastLoginEmail);
        Assert.Equal(0, h.DeviceStorageClears);
        Assert.Equal(0, h.Cache.ClearCalls);
        Assert.Equal(2, h.Cache.Items.Count);
        Assert.Equal(["//LoginPage"], h.Routes);
    }

    [Fact]
    public async Task ChangeSite_Declined_DoesNotRevokeOrClearAnything()
    {
        var h = new Harness { ConfirmChoice = false };

        await h.ShellVm.ChangeSiteCommand.ExecuteAsync(null);

        Assert.Empty(h.HttpHandler.SentRequests);
        Assert.Equal(0, h.Session.ClearSessionCalls);
        Assert.Equal(0, h.DeviceStorageClears);
        Assert.Equal(0, h.Cache.ClearCalls);
        Assert.Equal(2, h.Cache.Items.Count);
        Assert.NotNull(h.Session.Token);
        Assert.NotNull(h.TenantStore.CurrentTenant);
        Assert.True(h.PendingStore.TryGet(out _, out _));
        Assert.Empty(h.Routes);
    }

    [Fact]
    public async Task ChangeSite_Confirmed_RevokesToken_ClearsSessionTenantPreferencesAndOfflineCopies()
    {
        var h = new Harness { ConfirmChoice = true };
        OfflineState.Served(new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc));

        await h.ShellVm.ChangeSiteCommand.ExecuteAsync(null);

        Assert.Single(h.HttpHandler.SentRequests);
        Assert.EndsWith("/logout", h.HttpHandler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(1, h.DeviceStorageClears);
        Assert.Equal(1, h.Cache.ClearCalls);
        Assert.Empty(h.Cache.Items);
        Assert.Null(OfflineState.ServedAt);
        Assert.Equal(1, h.Session.ClearSessionCalls);
        Assert.Null(h.Session.Token);
        Assert.Null(h.Session.RefreshToken);
        Assert.Null(h.Session.CurrentUser);
        Assert.Equal(1, h.TenantStore.ClearMemoryCalls);
        Assert.Null(h.TenantStore.CurrentTenant);
        Assert.False(h.PendingStore.TryGet(out _, out _));
        Assert.Equal(["//ActivationPage"], h.Routes);
    }

    [Fact]
    public async Task SiteDataCleaner_ClearAll_ClearsOfflineResponseCacheDeviceStorageAndOfflineState()
    {
        var tenantStore = new FakeTenantStore
        {
            CurrentTenant = new TenantContext { Code = "DEM042", Host = "demo.example", Name = "Démo" }
        };
        var session = new FakeSessionService
        {
            Token = "jwt_actif",
            RefreshToken = "rt_actif",
            CurrentUser = new UserInfo { Id = 42, Email = "admin@example.test" }
        };
        var cache = new MemoryResponseCache();
        cache.Save("demo.example:42:gestion/statistiques", "{\"a\":1}", DateTime.UtcNow);
        cache.Save("demo.example:42:gestion/tresorerie", "{\"b\":2}", DateTime.UtcNow);
        OfflineState.Served(new DateTime(2026, 11, 12, 9, 30, 0, DateTimeKind.Utc));

        var deviceClears = 0;
        var cleaner = new SiteDataCleaner(tenantStore, session, cache)
        {
            ClearDeviceStorage = () => deviceClears++
        };

        await cleaner.ClearAllAsync();

        Assert.Equal(1, deviceClears);
        Assert.Equal(1, cache.ClearCalls);
        Assert.Empty(cache.Items);
        Assert.Null(OfflineState.ServedAt);
        Assert.Equal(1, session.ClearSessionCalls);
        Assert.Equal(1, tenantStore.ClearMemoryCalls);
        Assert.Null(tenantStore.CurrentTenant);
    }
}

