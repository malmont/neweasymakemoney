using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Incidents;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

public class BugReportTests
{
    private sealed class FakeEnvironment : IIncidentEnvironment
    {
        public string? AppVersion => "2.1.0+57";
        public string? Os => "android";
        public string? OsVersion => "14";
        public string? Device => "SM-S918W";
        public string? Locale => "fr-CA";
        public bool IsOnline => true;
        public string? CurrentLocation => "//accueil";
    }

    private sealed class FakeBugService : IBugReportService
    {
        public List<(string Description, string Screen, string Context, UploadFile? Screenshot)> Sent { get; } = new();
        public Exception? SendError { get; set; }
        public BugReportList Mine { get; set; } = new();
        public int Loads { get; private set; }

        public Task<BugReportResult> SendAsync(string description, string screen, string contextJson, UploadFile? screenshot)
        {
            if (SendError != null)
                return Task.FromException<BugReportResult>(SendError);
            Sent.Add((description, screen, contextJson, screenshot));
            return Task.FromResult(new BugReportResult { report = new BugReport { id = 12, description = description } });
        }

        public Task<BugReportList> GetMineAsync()
        {
            Loads++;
            return Task.FromResult(Mine);
        }
    }

    private static (BugReportViewModel Vm, FakeBugService Service) Create(string fileName = "capture.png", int size = 100)
    {
        var service = new FakeBugService();
        var vm = new BugReportViewModel(service, new FakeDialogService(), new FakeEnvironment())
        {
            PickImageAsync = () => Task.FromResult<(string, byte[])?>((fileName, new byte[size])),
        };
        return (vm, service);
    }

    private static ApiException Error(HttpStatusCode status, string code, JObject? body = null) =>
        new(new ApiError { Code = code, Message = "Refus.", Body = body }, status);

    [Fact]
    public void Seul_le_personnel_peut_signaler()
    {
        Assert.True(BugReportRules.CanReport(new[] { "ROLE_ADMIN" }));
        Assert.True(BugReportRules.CanReport(new[] { "ROLE_USER", "ROLE_POS_MANAGER" }));
        Assert.True(BugReportRules.CanReport(new[] { "ROLE_USER_POS" }));
        Assert.False(BugReportRules.CanReport(new[] { "ROLE_SUPPORT" }));
        Assert.False(BugReportRules.CanReport(new[] { "ROLE_USER_INTERNET" }));
        Assert.False(BugReportRules.CanReport(null));
    }

    [Fact]
    public void Etats_en_francais()
    {
        Assert.Equal("Reçu", new BugReport { status = "received" }.StatusText);
        Assert.Equal("En cours", new BugReport { status = "in_progress" }.StatusText);
        Assert.Equal("Corrigé", new BugReport { status = "fixed" }.StatusText);
        Assert.Equal("Ne sera pas corrigé", new BugReport { status = "wont_fix" }.StatusText);
        Assert.Equal("Transmis à l'équipe technique", new SupportPlatform { status = "transmitted" }.StatusText);
    }

    [Fact]
    public void Lecture_de_la_liste_au_format_du_serveur()
    {
        var json = "{\"reports\":[{\"id\":12,\"app\":\"gestion\",\"appVersion\":\"2.1.0\",\"screen\":\"Achats/Facture\",\"description\":\"Le total est faux\",\"status\":\"fixed\",\"answer\":\"Corrigé le 19.\",\"createdAt\":\"2026-11-18T15:02:11Z\",\"answeredAt\":\"2026-11-18T16:40:00Z\"}]}";
        var list = Newtonsoft.Json.JsonConvert.DeserializeObject<BugReportList>(json)!;
        var report = Assert.Single(list.reports);
        Assert.Equal("Corrigé", report.StatusText);
        Assert.True(report.HasAnswer);
        Assert.StartsWith("Signalement n° 12 · ", report.HeaderText);
        Assert.EndsWith("Achats/Facture", report.HeaderText);
    }

    [Fact]
    public void Description_de_10_a_2000_caracteres()
    {
        var (vm, _) = Create();
        vm.Description = "trop court";
        Assert.True(vm.CanSend);
        vm.Description = "court";
        Assert.False(vm.CanSend);
        vm.Description = new string('a', 2001);
        Assert.False(vm.CanSend);
    }

    [Fact]
    public async Task La_capture_exige_la_case_et_n_est_jamais_jointe_d_office()
    {
        var (vm, service) = Create();
        vm.Description = "Le total de la facture est faux";
        await vm.SendCommand.ExecuteAsync(null);
        Assert.Null(Assert.Single(service.Sent).Screenshot);

        vm.Description = "Le total de la facture est faux";
        await vm.PickScreenshotCommand.ExecuteAsync(null);
        Assert.True(vm.HasScreenshot);
        Assert.False(vm.CanSend);
        vm.NoCustomerDataChecked = true;
        Assert.True(vm.CanSend);
        await vm.SendCommand.ExecuteAsync(null);
        Assert.Equal("image/png", service.Sent[1].Screenshot!.ContentType);
        Assert.False(vm.HasScreenshot);
        Assert.False(vm.NoCustomerDataChecked);
    }

    [Fact]
    public async Task Une_capture_trop_lourde_ou_d_un_autre_format_est_refusee()
    {
        var (vm, _) = Create("capture.gif");
        await vm.PickScreenshotCommand.ExecuteAsync(null);
        Assert.False(vm.HasScreenshot);
        Assert.True(vm.HasScreenshotError);

        var (big, _) = Create("capture.jpg", (int)BugReportRules.MaxScreenshotBytes + 1);
        await big.PickScreenshotCommand.ExecuteAsync(null);
        Assert.False(big.HasScreenshot);
        Assert.True(big.HasScreenshotError);
    }

    [Fact]
    public async Task Envoi_201_merci_et_liste_relue_avec_le_contexte_de_la_liste_blanche()
    {
        var (vm, service) = Create();
        vm.SelectSectionCommand.Execute("Tresorerie");
        vm.Description = "  Le solde affiché est faux  ";
        await vm.SendCommand.ExecuteAsync(null);

        var sent = Assert.Single(service.Sent);
        Assert.Equal("Le solde affiché est faux", sent.Description);
        Assert.Equal("Tresorerie", sent.Screen);
        var context = JObject.Parse(sent.Context);
        Assert.Equal("SM-S918W", (string?)context["device"]);
        foreach (var property in context.Properties())
            Assert.Contains(property.Name, new[] { "os", "osVersion", "device", "locale", "online" });
        Assert.Equal("Merci, votre signalement n° 12 est transmis.", vm.ResultMessage);
        Assert.Equal(string.Empty, vm.Description);
        Assert.Equal(1, service.Loads);
    }

    [Fact]
    public async Task Refus_422_429_403()
    {
        var (vm, service) = Create();
        vm.Description = "Le total de la facture est faux";
        service.SendError = Error(HttpStatusCode.UnprocessableEntity, "VALIDATION_FAILED", JObject.Parse("{\"fields\":{\"description\":\"INVALID_LENGTH\"}}"));
        await vm.SendCommand.ExecuteAsync(null);
        Assert.True(vm.HasDescriptionError);

        service.SendError = Error(HttpStatusCode.TooManyRequests, "BUG_REPORT_LIMIT");
        await vm.SendCommand.ExecuteAsync(null);
        Assert.Equal("Trop de signalements aujourd'hui. Réessayez demain.", vm.ResultMessage);

        service.SendError = Error(HttpStatusCode.Forbidden, "FORBIDDEN");
        await vm.SendCommand.ExecuteAsync(null);
        Assert.Equal("Ce compte ne peut pas envoyer de signalement.", vm.ResultMessage);
    }
}
