using Easymakemoney.Models.Session;
using Easymakemoney.Models.Tenant;
using Easymakemoney.Services;
using Easymakemoney.Services.Auth;
using Easymakemoney.Services.Site;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.ViewModels.Startup;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

public class LoadingPageViewModelTests
{
    private static TenantContext DemoTenant() => new()
    {
        Code = "DEM042",
        Host = "demo.example",
        Name = "Démo"
    };

    private static (LoadingPageViewModel ViewModel, FakeTenantStore TenantStore, FakeSiteDataCleaner Cleaner, FakeSessionService Session, List<string> Routes) Create(
        FakeTokenRefresher? tokenRefresher = null,
        TestHttpMessageHandler? httpHandler = null)
    {
        var tenantStore = new FakeTenantStore();
        var handler = httpHandler ?? new TestHttpMessageHandler(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        var siteService = new SiteService(
            new HttpClient(handler) { BaseAddress = new Uri("https://demo.example/api/") },
            new FakeDeviceInfoService());
        var cleaner = new FakeSiteDataCleaner();
        var session = new FakeSessionService();
        var jwtService = new JwtService();
        var refresher = tokenRefresher ?? new FakeTokenRefresher(RefreshResult.NetworkError());
        var routes = new List<string>();

        var vm = new LoadingPageViewModel(
            tenantStore,
            siteService,
            cleaner,
            session,
            jwtService,
            refresher,
            autoStart: false)
        {
            NavigateAsync = route =>
            {
                routes.Add(route);
                return Task.CompletedTask;
            },
            ShowInfoAsync = (_, _) => Task.CompletedTask,
            RunOnMainThreadAsync = action => action(),
            StartInBackground = _ => { }
        };

        return (vm, tenantStore, cleaner, session, routes);
    }

    [Fact]
    public async Task Startup_NoTenant_NavigatesToActivationPage_WithoutRefreshOrClear()
    {
        var refresher = new FakeTokenRefresher(RefreshResult.Success("nouveau"));
        var (vm, tenantStore, cleaner, _, routes) = Create(tokenRefresher: refresher);

        await vm.RunStartupAsync();

        Assert.Equal(1, tenantStore.EnsureFirstRunCleanCalls);
        Assert.Equal(0, refresher.InvocationCount);
        Assert.Equal(0, cleaner.ClearCalls);
        Assert.Equal(["//ActivationPage"], routes);
    }

    [Fact]
    public async Task Startup_TenantWithoutSession_NavigatesToLoginPage_AndKeepsTenant()
    {
        var refresher = new FakeTokenRefresher(RefreshResult.Success("nouveau"));
        var (vm, tenantStore, cleaner, _, routes) = Create(tokenRefresher: refresher);
        tenantStore.CurrentTenant = DemoTenant();
        var backgroundLaunched = false;
        vm.StartInBackground = _ => backgroundLaunched = true;

        await vm.RunStartupAsync();

        Assert.True(backgroundLaunched);
        Assert.Equal(0, refresher.InvocationCount);
        Assert.Equal(0, cleaner.ClearCalls);
        Assert.NotNull(tenantStore.CurrentTenant);
        Assert.Equal("DEM042", tenantStore.CurrentTenant.Code);
        Assert.Equal(["//LoginPage"], routes);
    }

    [Fact]
    public async Task Startup_ValidToken_NavigatesToDashboardPage_WithoutRefresh()
    {
        var refresher = new FakeTokenRefresher(RefreshResult.Success("nouveau"));
        var (vm, tenantStore, _, session, routes) = Create(tokenRefresher: refresher);
        tenantStore.CurrentTenant = DemoTenant();
        session.Token = TestJwtFactory.Create(expiresIn: TimeSpan.FromMinutes(15));
        session.RefreshToken = "refresh_valide";
        session.CurrentUser = new UserInfo { Id = 42, Email = "admin@example.test" };

        await vm.RunStartupAsync();

        Assert.Equal(0, refresher.InvocationCount);
        Assert.Equal(["//DashboardPage"], routes);
    }

    [Fact]
    public async Task Startup_ExpiredToken_RefreshSucceeded_NavigatesToDashboardPage()
    {
        var refresher = new FakeTokenRefresher(RefreshResult.Success("jeton_renouvele"));
        var (vm, tenantStore, _, session, routes) = Create(tokenRefresher: refresher);
        tenantStore.CurrentTenant = DemoTenant();
        session.Token = TestJwtFactory.Create(expiresIn: TimeSpan.FromMinutes(-5));
        session.RefreshToken = "refresh_valide";
        session.CurrentUser = new UserInfo { Id = 42, Email = "admin@example.test" };

        await vm.RunStartupAsync();

        Assert.Equal(1, refresher.InvocationCount);
        Assert.Equal(["//DashboardPage"], routes);
    }

    [Fact]
    public async Task Startup_ExpiredToken_RefreshRejected_NavigatesToLoginPage()
    {
        var refresher = new FakeTokenRefresher(RefreshResult.Rejected());
        var (vm, tenantStore, _, session, routes) = Create(tokenRefresher: refresher);
        tenantStore.CurrentTenant = DemoTenant();
        session.Token = TestJwtFactory.Create(expiresIn: TimeSpan.FromMinutes(-5));
        session.RefreshToken = "refresh_revoque";

        await vm.RunStartupAsync();

        Assert.Equal(1, refresher.InvocationCount);
        Assert.Equal(["//LoginPage"], routes);
    }

    [Fact]
    public async Task Startup_ExpiredToken_NetworkError_KeepsSessionAndNavigatesToDashboardPage()
    {
        var refresher = new FakeTokenRefresher(RefreshResult.NetworkError());
        var (vm, tenantStore, cleaner, session, routes) = Create(tokenRefresher: refresher);
        var originalToken = TestJwtFactory.Create(expiresIn: TimeSpan.FromMinutes(-5));
        tenantStore.CurrentTenant = DemoTenant();
        session.Token = originalToken;
        session.RefreshToken = "refresh_valide";
        session.CurrentUser = new UserInfo { Id = 42, Email = "admin@example.test" };

        await vm.RunStartupAsync();

        Assert.Equal(1, refresher.InvocationCount);
        Assert.Equal(0, cleaner.ClearCalls);
        Assert.Equal(0, session.ClearSessionCalls);
        Assert.Equal(originalToken, session.Token);
        Assert.Equal("refresh_valide", session.RefreshToken);
        Assert.NotNull(session.CurrentUser);
        Assert.NotNull(tenantStore.CurrentTenant);
        Assert.Equal(["//DashboardPage"], routes);
    }
}

