using System.Net;
using System.Text;
using Easymakemoney.Models.Tenant;
using Easymakemoney.Services;
using Easymakemoney.Services.Auth;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCases;
using Easymakemoney.ViewModels.Startup;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

public class LoginPageViewModelTests
{
    private static (LoginPageViewModel ViewModel, FakeTenantStore TenantStore, FakeSessionService Session, PendingLoginStore PendingStore, List<string> Routes) CreateWithHttp(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new TestHttpMessageHandler(responder);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://demo.example/api/") };
        var loginService = new LoginService(new FakeHttpClientFactory(client));
        return Create(loginService);
    }

    private static (LoginPageViewModel ViewModel, FakeTenantStore TenantStore, FakeSessionService Session, PendingLoginStore PendingStore, List<string> Routes) Create(
        ILoginService loginService,
        string? initialLastLoginEmail = null)
    {
        var tenantStore = new FakeTenantStore
        {
            CurrentTenant = new TenantContext { Code = "DEM042", Host = "demo.example", Name = "Démo" }
        };
        var session = new FakeSessionService { LastLoginEmail = initialLastLoginEmail };
        var notifier = new FakeSessionExpiryNotifier();
        var useCase = new LoginUseCase(loginService, new JwtService(), tenantStore, session, notifier);
        var pendingStore = new PendingLoginStore();
        var authFlow = new FakeAuthFlowService();
        var routes = new List<string>();

        var vm = new LoginPageViewModel(useCase, authFlow, pendingStore, session)
        {
            NavigateAsync = route =>
            {
                routes.Add(route);
                return Task.CompletedTask;
            }
        };

        return (vm, tenantStore, session, pendingStore, routes);
    }

    [Fact]
    public async Task Login_EmptyFields_ShowsError_WithoutNetworkCall()
    {
        var fakeLogin = new FakeLoginService();
        var (vm, _, _, _, routes) = Create(fakeLogin);
        vm.Email = "";
        vm.Password = "secret";

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Empty(fakeLogin.AuthenticateCalls);
        Assert.Equal("Veuillez saisir votre adresse e-mail et votre mot de passe.", vm.ErrorMessage);
        Assert.True(vm.HasErrorMessage);
        Assert.Empty(routes);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShowsServerError_AndNoSessionSaved()
    {
        var (vm, _, session, _, routes) = CreateWithHttp(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"message\":\"Identifiants incorrects.\"}", Encoding.UTF8, "application/json")
        });
        vm.Email = "admin@example.test";
        vm.Password = "mauvais-mot-de-passe";

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Identifiants incorrects.", vm.ErrorMessage);
        Assert.True(vm.HasErrorMessage);
        Assert.Null(session.Token);
        Assert.Null(session.RefreshToken);
        Assert.Null(session.CurrentUser);
        Assert.Empty(routes);
    }

    [Fact]
    public async Task Login_NonAdminAccount_RefusedWithManagementMessage()
    {
        // Cas 1 : refus serveur 403 PLATFORM_NOT_ALLOWED
        var (vm403, _, session403, _, routes403) = CreateWithHttp(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("{\"code\":\"PLATFORM_NOT_ALLOWED\",\"message\":\"Plateforme refusée par le serveur.\"}", Encoding.UTF8, "application/json")
        });
        vm403.Email = "vendeur@example.test";
        vm403.Password = "secret";

        await vm403.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Ce compte n'a pas accès à l'application de gestion.", vm403.ErrorMessage);
        Assert.Null(session403.Token);
        Assert.Empty(routes403);

        // Cas 2 : jeton 200 sans ROLE_ADMIN (ex. ROLE_SUPPORT) refusé localement par JwtService
        var supportJwt = TestJwtFactory.Create(tenantCode: "DEM042", email: "support@example.test", roles: ["ROLE_SUPPORT", "ROLE_USER"]);
        var body = $"{{\"token\":\"{supportJwt}\",\"refresh_token\":\"rt_support\"}}";
        var (vmJwt, _, sessionJwt, _, routesJwt) = CreateWithHttp(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
        vmJwt.Email = "support@example.test";
        vmJwt.Password = "secret";

        await vmJwt.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Ce compte n'a pas accès à l'application de gestion.", vmJwt.ErrorMessage);
        Assert.Null(sessionJwt.Token);
        Assert.Empty(routesJwt);
    }

    [Fact]
    public async Task Login_OtpRequired_StoresPendingLogin_ClearsPassword_AndNavigatesToOtp()
    {
        var (vm, _, session, pendingStore, routes) = CreateWithHttp(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"otp_required\":true}", Encoding.UTF8, "application/json")
        });
        vm.Email = "admin@example.test";
        vm.Password = "MotDePasseTest123";

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.True(pendingStore.TryGet(out var storedEmail, out var storedPassword));
        Assert.Equal("admin@example.test", storedEmail);
        Assert.Equal("MotDePasseTest123", storedPassword);
        Assert.Equal(string.Empty, vm.Password);
        Assert.Null(session.Token);
        Assert.Equal(["//OtpPage"], routes);
    }

    [Fact]
    public async Task Initialize_PreFillsLastLoginEmail_WithoutOverwritingExistingInput()
    {
        var fakeLogin = new FakeLoginService();
        var (vm, _, _, _, _) = Create(fakeLogin, initialLastLoginEmail: "admin@example.test");

        await vm.InitializeAsync();
        Assert.Equal("admin@example.test", vm.Email);

        vm.Email = "autre@example.test";
        await vm.InitializeAsync();
        Assert.Equal("autre@example.test", vm.Email);
    }
}
