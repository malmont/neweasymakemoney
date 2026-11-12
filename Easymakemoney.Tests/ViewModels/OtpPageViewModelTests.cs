using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Easymakemoney.Models.Tenant;
using Easymakemoney.Services;
using Easymakemoney.Services.Auth;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCases;
using Easymakemoney.ViewModels.Startup;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

public class OtpPageViewModelTests
{
    private static (OtpPageViewModel ViewModel, FakeSessionService Session, PendingLoginStore PendingStore, List<string> Routes, List<(string Title, string Message)> Infos) CreateWithHttp(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        Func<DateTime>? clock = null)
    {
        var handler = new TestHttpMessageHandler(responder);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://demo.example/api/") };
        return Create(new LoginService(new FakeHttpClientFactory(client)), clock);
    }

    private static (OtpPageViewModel ViewModel, FakeSessionService Session, PendingLoginStore PendingStore, List<string> Routes, List<(string Title, string Message)> Infos) Create(
        ILoginService loginService,
        Func<DateTime>? clock = null)
    {
        var tenantStore = new FakeTenantStore
        {
            CurrentTenant = new TenantContext { Code = "DEM042", Host = "demo.example", Name = "Démo" }
        };
        var session = new FakeSessionService();
        var notifier = new FakeSessionExpiryNotifier();
        var useCase = new LoginUseCase(loginService, new JwtService(), tenantStore, session, notifier);
        var pendingStore = new PendingLoginStore();
        if (clock != null)
        {
            pendingStore.UtcNow = clock;
        }

        var routes = new List<string>();
        var infos = new List<(string Title, string Message)>();

        var vm = new OtpPageViewModel(useCase, pendingStore)
        {
            NavigateAsync = route =>
            {
                routes.Add(route);
                return Task.CompletedTask;
            },
            ShowInfoAsync = (title, message) =>
            {
                infos.Add((title, message));
                return Task.CompletedTask;
            }
        };

        return (vm, session, pendingStore, routes, infos);
    }

    [Fact]
    public async Task VerifyOtp_BlankCode_ShowsError_WithoutNetworkCall()
    {
        var fakeLogin = new FakeLoginService();
        var (vm, _, pendingStore, routes, _) = Create(fakeLogin);
        pendingStore.Set("admin@example.test", "secret");
        await vm.InitializeAsync();
        vm.OtpCode = "   ";

        await vm.VerifyOtpCommand.ExecuteAsync(null);

        Assert.Empty(fakeLogin.VerifyOtpCalls);
        Assert.Equal("Veuillez saisir le code reçu par courriel.", vm.ErrorMessage);
        Assert.True(vm.HasErrorMessage);
        Assert.Empty(routes);
    }

    [Fact]
    public async Task VerifyOtp_IncompleteOrWrongCode_ShowsError_AndPreservesPendingLogin()
    {
        var (vm, session, pendingStore, routes, _) = CreateWithHttp(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"code\":\"OTP_INVALID\",\"message\":\"Code incorrect ou expiré.\"}", Encoding.UTF8, "application/json")
        });
        pendingStore.Set("admin@example.test", "secret");
        await vm.InitializeAsync();
        vm.OtpCode = "12";

        await vm.VerifyOtpCommand.ExecuteAsync(null);

        Assert.Equal("Code incorrect ou expiré.", vm.ErrorMessage);
        Assert.True(vm.HasErrorMessage);
        Assert.Null(session.Token);
        Assert.True(pendingStore.TryGet(out var email, out var password));
        Assert.Equal("admin@example.test", email);
        Assert.Equal("secret", password);
        Assert.Empty(routes);
    }

    [Fact]
    public async Task VerifyOtp_ExpiredPendingStore_NavigatesBackToLogin_WithoutServiceCall()
    {
        var now = new DateTime(2026, 11, 12, 12, 0, 0, DateTimeKind.Utc);
        var fakeLogin = new FakeLoginService();
        var (vm, _, pendingStore, routes, _) = Create(fakeLogin, () => now);
        pendingStore.Set("admin@example.test", "secret");
        vm.OtpCode = "123456";

        now = now.AddMinutes(11);
        await vm.VerifyOtpCommand.ExecuteAsync(null);

        Assert.Empty(fakeLogin.VerifyOtpCalls);
        Assert.Equal(["//LoginPage"], routes);
    }

    [Fact]
    public async Task ResendOtp_SuccessAndRateLimited429_HandlesDelayWithoutWaiting()
    {
        var now = new DateTime(2026, 11, 12, 12, 0, 0, DateTimeKind.Utc);
        var returnRateLimit = false;
        var (vm, _, pendingStore, routes, infos) = CreateWithHttp(_ =>
        {
            if (!returnRateLimit)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"otp_required\":true}", Encoding.UTF8, "application/json")
                };
            }

            var response = new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent("{\"code\":\"TOO_MANY_ATTEMPTS\"}", Encoding.UTF8, "application/json")
            };
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(45));
            return response;
        }, () => now);

        pendingStore.Set("admin@example.test", "secret");
        await vm.InitializeAsync();
        vm.OtpCode = "99";
        now = now.AddMinutes(9);

        // Renvoi réussi : vide la saisie, renouvelle la fenêtre de 10 min et affiche la confirmation
        await vm.ResendOtpCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, vm.OtpCode);
        Assert.False(vm.HasErrorMessage);
        Assert.Single(infos);
        Assert.Equal("Code envoyé", infos[0].Title);
        Assert.Contains("admin@example.test", infos[0].Message);

        // 9 minutes plus tard (18 min depuis le 1er envoi) : encore valide grâce au renouvellement
        now = now.AddMinutes(9);
        returnRateLimit = true;
        await vm.ResendOtpCommand.ExecuteAsync(null);

        Assert.Equal("Trop de tentatives. Veuillez patienter 45 secondes.", vm.ErrorMessage);
        Assert.True(vm.HasErrorMessage);
        Assert.Empty(routes);
    }

    [Fact]
    public async Task VerifyOtp_ValidCode_SavesSessionAndLastEmail_ClearsPending_NavigatesToDashboard()
    {
        var jwt = TestJwtFactory.Create(tenantCode: "DEM042", email: "admin@example.test", userId: 77);
        var body = $"{{\"token\":\"{jwt}\",\"refresh_token\":\"rt_admin_77\"}}";
        var (vm, session, pendingStore, routes, _) = CreateWithHttp(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
        pendingStore.Set("admin@example.test", "secret");
        await vm.InitializeAsync();
        vm.OtpCode = "123456";

        await vm.VerifyOtpCommand.ExecuteAsync(null);

        Assert.Equal(jwt, session.Token);
        Assert.Equal("rt_admin_77", session.RefreshToken);
        Assert.NotNull(session.CurrentUser);
        Assert.Equal("admin@example.test", session.CurrentUser.Email);
        Assert.Equal("admin@example.test", session.LastLoginEmail);
        Assert.False(pendingStore.TryGet(out _, out _));
        Assert.Equal(string.Empty, vm.OtpCode);
        Assert.False(vm.HasErrorMessage);
        Assert.Equal(["//DashboardPage"], routes);
    }
}
