using System.Globalization;
using System.Net;
using System.Text;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;
using Easymakemoney.Services.Module;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.ViewModels.Lists;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Lot MOD : module de gestion activable (403 MODULE_INACTIVE, demande d'essai, unités IA).</summary>
public class ManagementModuleTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public ManagementModuleTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
    }

    public void Dispose() => CultureInfo.CurrentCulture = _originalCulture;

    private const string InactiveJson = """
    { "error": "MODULE_INACTIVE", "code": "MODULE_INACTIVE", "module": "management", "feature": "app.management",
      "message": "Module inactif", "requestable": true, "request": null, "trial": { "allowed": true, "days": 30 } }
    """;

    // ---------- Lecture du 403 ----------

    [Fact]
    public void TryParse_ManagementWithTrial_ReadsAllFields()
    {
        var info = ModuleInactiveInfo.TryParse(InactiveJson);

        Assert.NotNull(info);
        Assert.True(info!.IsManagement);
        Assert.Equal("app.management", info.Feature);
        Assert.True(info.Requestable);
        Assert.False(info.Pending);
        Assert.Equal(30, info.TrialDays);
        Assert.True(info.CanRequest);
        Assert.Equal("Essayer 30 jours gratuitement", info.RequestButtonText);
        Assert.Contains("30 jours", info.Explanation);
    }

    [Fact]
    public void TryParse_PendingRequest_HidesRequestButton()
    {
        var info = ModuleInactiveInfo.TryParse("""
        { "code": "MODULE_INACTIVE", "module": "management", "requestable": true, "request": { "id": 4, "status": "pending" }, "trial": { "allowed": false, "days": 0 } }
        """);

        Assert.True(info!.Pending);
        Assert.False(info.CanRequest);
        Assert.Equal(0, info.TrialDays);
        Assert.Contains("en attente", info.Explanation);
    }

    [Fact]
    public void TryParse_NotRequestable_NoButton()
    {
        var info = ModuleInactiveInfo.TryParse("""{ "code": "MODULE_INACTIVE", "module": "management", "requestable": false }""");

        Assert.False(info!.CanRequest);
        Assert.Contains("Contactez la plateforme", info.Explanation);
    }

    [Fact]
    public void TryParse_TrialNotAllowed_AsksActivation()
    {
        var info = ModuleInactiveInfo.TryParse("""{ "code": "MODULE_INACTIVE", "module": "management", "trial": { "allowed": false, "days": 30 } }""");

        Assert.Equal(0, info!.TrialDays);
        Assert.Equal("Demander l'activation", info.RequestButtonText);
    }

    [Fact]
    public void TryParse_MinimalOldShape_DefaultsToRequestable()
    {
        // Forme actuelle des autres modules : { error, code, module, message } sans requestable ni trial.
        var info = ModuleInactiveInfo.TryParse("""{ "error": "MODULE_INACTIVE", "code": "MODULE_INACTIVE", "module": "management", "message": "x" }""");

        Assert.True(info!.CanRequest);
        Assert.Equal(0, info.TrialDays);
    }

    [Theory]
    [InlineData("""{ "code": "MODULE_INACTIVE", "module": "boutique" }""", false)]
    [InlineData("""{ "code": "FORBIDDEN", "module": "management" }""", null)]
    [InlineData("""{ "code": "SUPER_ADMIN_REQUIRED" }""", null)]
    [InlineData("pas du json", null)]
    [InlineData("", null)]
    public void TryParse_OtherResponses(string json, bool? isManagement)
    {
        var info = ModuleInactiveInfo.TryParse(json);

        if (isManagement == null)
            Assert.Null(info);
        else
            Assert.Equal(isManagement, info!.IsManagement);
    }

    // ---------- Intercepteur ----------

    [Fact]
    public async Task GateHandler_Management403_NotifiesAndKeepsBodyReadable()
    {
        var notifier = new FakeModuleInactiveNotifier();
        var client = ClientReturning(HttpStatusCode.Forbidden, InactiveJson, notifier);

        var response = await client.GetAsync("https://exemple.test/api/gestion/statistiques");

        Assert.Single(notifier.Notified);
        Assert.Equal(30, notifier.Notified[0].TrialDays);
        // HttpService relit le corps pour construire l'ApiException
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("MODULE_INACTIVE", ApiError.FromHttpResponse(body, response.StatusCode, response.Headers).Code);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, """{ "code": "MODULE_INACTIVE", "module": "boutique" }""")]
    [InlineData(HttpStatusCode.Forbidden, """{ "code": "SUPER_ADMIN_REQUIRED", "message": "x" }""")]
    [InlineData(HttpStatusCode.OK, InactiveJson)]
    [InlineData(HttpStatusCode.NotFound, """{ "code": "NOT_FOUND" }""")]
    public async Task GateHandler_OtherResponses_DoNotNotify(HttpStatusCode status, string json)
    {
        var notifier = new FakeModuleInactiveNotifier();
        var client = ClientReturning(status, json, notifier);

        var response = await client.GetAsync("https://exemple.test/api/x");

        Assert.Empty(notifier.Notified);
        Assert.Equal(status, response.StatusCode);
    }

    [Fact]
    public async Task DialogService_ModuleInactive_StaysSilent()
    {
        var error = ApiError.FromHttpResponse(InactiveJson, HttpStatusCode.Forbidden, null);

        // Pas de Shell ni de MainThread dans les tests : un retour immédiat prouve que rien n'est affiché.
        await new DialogService().ShowErrorAsync(new ApiException(error, HttpStatusCode.Forbidden));
    }

    // ---------- Demande d'essai ----------

    [Fact]
    public async Task Service_Request_PostsToFeatureRoute()
    {
        var http = new FakeHttpService { ResultToReturn = new FeatureRequestResult { kind = "request" } };

        var result = await new ManagementModuleService(http).RequestAsync();

        Assert.Equal("site/features/app.management/request", http.PostedAuthData.Single().Url);
        Assert.Equal("request", result.kind);
    }

    [Fact]
    public async Task Service_GetAi_ReadsGestionIa()
    {
        var http = new FakeHttpService { ResultToReturn = new ManagementAi { tier = "help" } };

        await new ManagementModuleService(http).GetAiAsync();

        Assert.Equal("gestion/ia", http.RequestedUrls.Single());
    }

    [Fact]
    public void RequestResult_Trial_ReadsEndDate()
    {
        var result = Newtonsoft.Json.JsonConvert.DeserializeObject<FeatureRequestResult>("""
        { "kind": "trial", "entitlement": { "id": 9, "source": "trial", "endsAt": "2026-12-11T23:59:59+00:00" }, "request": null }
        """)!;

        Assert.True(result.IsTrial);
        Assert.NotNull(result.TrialEndsAt);
        Assert.StartsWith("Essai ouvert jusqu'au", result.ConfirmationText);
    }

    [Fact]
    public void RequestResult_Request_SaysSent()
    {
        var result = Newtonsoft.Json.JsonConvert.DeserializeObject<FeatureRequestResult>("""{ "kind": "request", "request": { "id": 3 } }""")!;

        Assert.False(result.IsTrial);
        Assert.StartsWith("Demande envoyée", result.ConfirmationText);
    }

    [Fact]
    public async Task ViewModel_Trial_ResetsAndGoesHome()
    {
        var (vm, service, notifier, dialogs, home) = CreateViewModel();
        service.Result = new FeatureRequestResult { kind = "trial" };

        await vm.RequestCommand.ExecuteAsync(null);

        Assert.Equal("app.management", service.Requested.Single());
        Assert.Equal(1, notifier.Resets);
        Assert.Equal(1, home.Count);
        Assert.Single(dialogs.ShownInfos);
        Assert.Empty(dialogs.ShownErrors);
    }

    [Fact]
    public async Task ViewModel_Request_BecomesPendingAndStays()
    {
        var (vm, service, _, dialogs, home) = CreateViewModel();
        service.Result = new FeatureRequestResult { kind = "request" };

        await vm.RequestCommand.ExecuteAsync(null);

        Assert.True(vm.Info.Pending);
        Assert.False(vm.CanRequest);
        Assert.Equal(0, home.Count);
        Assert.StartsWith("Demande envoyée", dialogs.ShownInfos.Single());
    }

    [Fact]
    public async Task ViewModel_AlreadyEnabled_GoesHomeWithoutError()
    {
        var (vm, service, notifier, dialogs, home) = CreateViewModel();
        service.Error = Conflict("ALREADY_ENABLED");

        await vm.RequestCommand.ExecuteAsync(null);

        Assert.Equal(1, home.Count);
        Assert.Equal(1, notifier.Resets);
        Assert.Empty(dialogs.ShownErrors);
    }

    [Fact]
    public async Task ViewModel_RequestPending_BecomesPendingWithoutError()
    {
        var (vm, service, _, dialogs, home) = CreateViewModel();
        service.Error = Conflict("REQUEST_PENDING");

        await vm.RequestCommand.ExecuteAsync(null);

        Assert.True(vm.Info.Pending);
        Assert.Equal(0, home.Count);
        Assert.Empty(dialogs.ShownErrors);
    }

    [Fact]
    public async Task ViewModel_PendingFromServer_SendsNothing()
    {
        var (vm, service, _, _, _) = CreateViewModel("""{ "code": "MODULE_INACTIVE", "module": "management", "request": { "id": 1 } }""");

        await vm.RequestCommand.ExecuteAsync(null);

        Assert.Empty(service.Requested);
    }

    [Fact]
    public async Task ViewModel_Retry_ResetsAndGoesHome()
    {
        var (vm, service, notifier, _, home) = CreateViewModel();

        await vm.RetryCommand.ExecuteAsync(null);

        Assert.Equal(1, notifier.Resets);
        Assert.Equal(1, home.Count);
        Assert.Empty(service.Requested);
    }

    // ---------- Unités IA ----------

    [Fact]
    public void Ai_Included_ShowsRemainingCredits()
    {
        var ai = Newtonsoft.Json.JsonConvert.DeserializeObject<ManagementAi>("""
        { "tier": "help", "credits": { "monthly": 100, "used": 37, "remaining": 63, "resetAt": "2026-12-01T00:00:00+00:00" },
          "costs": { "advice": 1, "comment": 1, "document": 1 }, "limits": { "commentsPerDay": 10, "adviceRefreshPerDay": 3 },
          "today": { "comments": 2, "adviceRefresh": 0 } }
        """)!;

        Assert.True(ai.IsIncluded);
        Assert.False(ai.IsExhausted);
        Assert.StartsWith("Unités IA : 63 sur 100 ce mois-ci", ai.CreditsText);
        Assert.Equal(10, ai.limits!.commentsPerDay);
    }

    [Fact]
    public void Ai_None_SaysNotIncluded()
    {
        var ai = new ManagementAi { tier = "none" };

        Assert.False(ai.IsIncluded);
        Assert.Contains("non incluse", ai.CreditsText);
    }

    [Fact]
    public void Ai_Exhausted()
    {
        var ai = new ManagementAi { tier = "full", credits = new ManagementAiCredits { monthly = 50, used = 50, remaining = 0 } };

        Assert.True(ai.IsExhausted);
    }

    [Fact]
    public async Task Advice_AiRouteMissing_HidesLineWithoutError()
    {
        var dialogs = new FakeDialogService();
        var vm = new AdviceViewModel(new NoAdviceService(), dialogs, new FakeManagementModuleService
        {
            Error = new ApiException(ApiError.FromHttpResponse("""{ "code": "NOT_FOUND" }""", HttpStatusCode.NotFound, null), HttpStatusCode.NotFound),
        });

        await vm.LoadAiAsync();

        Assert.False(vm.HasAi);
        Assert.Empty(dialogs.ShownErrors);
    }

    [Fact]
    public async Task Advice_AiAvailable_ShowsLine()
    {
        var vm = new AdviceViewModel(new NoAdviceService(), new FakeDialogService(), new FakeManagementModuleService
        {
            Ai = new ManagementAi { tier = "help", credits = new ManagementAiCredits { monthly = 100, remaining = 80 } },
        });

        await vm.LoadAiAsync();

        Assert.True(vm.HasAi);
        Assert.StartsWith("Unités IA : 80 sur 100", vm.AiCreditsText);
    }

    // ---------- Outils ----------

    private static HttpClient ClientReturning(HttpStatusCode status, string json, IModuleInactiveNotifier notifier) =>
        new(new ModuleGateHandler(notifier)
        {
            InnerHandler = new TestHttpMessageHandler(_ => new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }),
        });

    private static ApiException Conflict(string code) =>
        new(ApiError.FromHttpResponse($$"""{ "code": "{{code}}", "message": "x" }""", HttpStatusCode.Conflict, null), HttpStatusCode.Conflict);

    private static (ModuleInactiveViewModel, FakeManagementModuleService, FakeModuleInactiveNotifier, FakeDialogService, Counter) CreateViewModel(string json = InactiveJson)
    {
        var service = new FakeManagementModuleService();
        var notifier = new FakeModuleInactiveNotifier { Last = ModuleInactiveInfo.TryParse(json) };
        var dialogs = new FakeDialogService();
        var home = new Counter();
        var vm = new ModuleInactiveViewModel(service, notifier, dialogs)
        {
            GoHomeAsync = () => { home.Count++; return Task.CompletedTask; },
        };
        return (vm, service, notifier, dialogs, home);
    }

    private sealed class Counter
    {
        public int Count { get; set; }
    }

    private sealed class FakeModuleInactiveNotifier : IModuleInactiveNotifier
    {
        public List<ModuleInactiveInfo> Notified { get; } = new();
        public int Resets { get; private set; }
        public ModuleInactiveInfo? Last { get; set; }

        public Task NotifyAsync(ModuleInactiveInfo info)
        {
            Notified.Add(info);
            Last = info;
            return Task.CompletedTask;
        }

        public void Reset() => Resets++;
    }

    internal sealed class FakeManagementModuleService : IManagementModuleService
    {
        public List<string> Requested { get; } = new();
        public FeatureRequestResult Result { get; set; } = new() { kind = "request" };
        public ManagementAi Ai { get; set; } = new();
        public Exception? Error { get; set; }

        public Task<FeatureRequestResult> RequestAsync(string feature = ModuleInactiveInfo.ManagementFeature, string? message = null)
        {
            Requested.Add(feature);
            return Error != null ? Task.FromException<FeatureRequestResult>(Error) : Task.FromResult(Result);
        }

        public Task<ManagementAi> GetAiAsync() =>
            Error != null ? Task.FromException<ManagementAi>(Error) : Task.FromResult(Ai);

        public SiteReadiness Readiness { get; set; } = new();

        public Task<SiteReadiness> GetReadinessAsync() =>
            Error != null ? Task.FromException<SiteReadiness>(Error) : Task.FromResult(Readiness);
    }

    private sealed class NoAdviceService : IAdviceService
    {
        public Task<AdviceList> GetAsync(int? periodId = null) => Task.FromResult(new AdviceList());
        public Task DoneAsync(int adviceId, string? note = null, IDictionary<string, object>? applied = null) => Task.CompletedTask;
        public Task DismissAsync(int adviceId) => Task.CompletedTask;
        public Task RefreshAsync(int? periodId = null) => Task.CompletedTask;
    }
}
