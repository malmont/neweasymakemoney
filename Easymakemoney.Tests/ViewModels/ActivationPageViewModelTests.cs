using System.Net;
using System.Text;
using Easymakemoney.Services.Site;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.ViewModels.Startup;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

public class ActivationPageViewModelTests
{
    private static (ActivationPageViewModel ViewModel, TestHttpMessageHandler Handler, FakeTenantStore TenantStore, FakeSiteDataCleaner Cleaner, List<string> Routes) Create(
        Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
    {
        var handler = new TestHttpMessageHandler(responder ?? (_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var siteService = new SiteService(
            new HttpClient(handler) { BaseAddress = new Uri("https://demo.example/api/") },
            new FakeDeviceInfoService());
        var tenantStore = new FakeTenantStore();
        var cleaner = new FakeSiteDataCleaner();
        var routes = new List<string>();

        var vm = new ActivationPageViewModel(siteService, tenantStore, cleaner)
        {
            NavigateAsync = route =>
            {
                routes.Add(route);
                return Task.CompletedTask;
            }
        };

        return (vm, handler, tenantStore, cleaner, routes);
    }

    [Fact]
    public async Task ValidateSiteCode_EmptyOrWhitespace_ShowsError_WithoutHttpCall()
    {
        var (vm, handler, _, _, _) = Create();
        vm.SiteCodeInput = "   ";

        await vm.ValidateSiteCodeCommand.ExecuteAsync(null);

        Assert.Empty(handler.SentRequests);
        Assert.Equal("Saisissez le code du site.", vm.ErrorMessage);
        Assert.True(vm.HasErrorMessage);
        Assert.False(vm.IsConfirming);
    }

    [Fact]
    public async Task ValidateKey_Malformed_ShowsError_WithoutHttpCall()
    {
        var (vm, handler, _, _, _) = Create();
        vm.KeyInput = "CLE-INVALIDE";

        await vm.ValidateKeyCommand.ExecuteAsync(null);

        Assert.Empty(handler.SentRequests);
        Assert.False(vm.IsConfirming);
        Assert.True(vm.HasErrorMessage);
        Assert.Contains("IZ-ADM", vm.ErrorMessage);
    }

    [Fact]
    public async Task ValidateSiteCode_Server404NotFound_ShowsErrorMessage()
    {
        var (vm, handler, _, _, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"code\":\"TENANT_NOT_FOUND\"}", Encoding.UTF8, "application/json")
        });
        vm.SiteCodeInput = "INCONNU";

        await vm.ValidateSiteCodeCommand.ExecuteAsync(null);

        Assert.Single(handler.SentRequests);
        Assert.Equal("Site introuvable.", vm.ErrorMessage);
        Assert.True(vm.HasErrorMessage);
        Assert.False(vm.IsConfirming);
    }

    [Fact]
    public async Task ValidateKey_Accepted_SetsCandidateAndConfirmationMode()
    {
        const string json = "{\"tenant\":{\"host\":\"demo.example\",\"code\":\"DEM042\",\"name\":\"Démo\"},\"label\":\"Appareil Test\"}";
        var (vm, handler, tenantStore, _, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        vm.KeyInput = "IZ-ADM-ABCD-1234-EFGH";

        await vm.ValidateKeyCommand.ExecuteAsync(null);

        Assert.Single(handler.SentRequests);
        Assert.True(vm.IsConfirming);
        Assert.NotNull(vm.CandidateTenant);
        Assert.Equal("DEM042", vm.CandidateTenant.Code);
        Assert.Equal("demo.example", vm.CandidateTenant.Host);
        Assert.Equal("Démo", vm.CandidateTenant.Name);
        Assert.Null(tenantStore.CurrentTenant);
        Assert.False(vm.HasErrorMessage);
    }

    [Fact]
    public async Task ConfirmTenant_ClearsPreviousData_SavesTenant_AndNavigatesToLogin()
    {
        const string json = "{\"tenant\":{\"host\":\"demo.example\",\"code\":\"DEM042\",\"name\":\"Démo\"}}";
        var (vm, _, tenantStore, cleaner, routes) = Create(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        vm.KeyInput = "IZ-ADM-ABCD-1234-EFGH";
        await vm.ValidateKeyCommand.ExecuteAsync(null);

        await vm.ConfirmTenantCommand.ExecuteAsync(null);

        Assert.Equal(1, cleaner.ClearCalls);
        Assert.Equal(1, tenantStore.SaveCalls);
        Assert.NotNull(tenantStore.CurrentTenant);
        Assert.Equal("DEM042", tenantStore.CurrentTenant.Code);
        Assert.False(vm.IsConfirming);
        Assert.False(vm.HasErrorMessage);
        Assert.Equal(["//LoginPage"], routes);
    }
}

