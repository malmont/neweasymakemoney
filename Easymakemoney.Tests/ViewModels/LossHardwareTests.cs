using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

/// <summary>
/// Matériel des pertes sur le mobile (L3-a, L3-a2) : tableau de santé relu sans prolonger la session, boîtiers,
/// jumelage par QR code ou code tapé. Exemples du backend : tests/Fixtures/pertes/boitier/admin-*.json.
/// </summary>
public class LossHardwareTests
{
    // admin-sante-imprimante.json (corps)
    private const string HealthJson = """
        {"devices": [{"code": "box-3", "kind": "box", "label": "Boîtier de l'arrière-boutique", "zone": null, "cashRegisterId": null, "state": "ok", "stateLabel": "En ordre", "lastSeenAt": "2026-11-16T11:15:44+00:00", "silentSince": null, "silentSeconds": null, "hint": null},
                     {"code": "printer-4", "kind": "printer", "label": "Imprimante à puce", "zone": null, "cashRegisterId": null, "state": "down", "stateLabel": "En panne ou débranché", "lastSeenAt": "2026-11-16T11:15:44+00:00", "silentSince": null, "silentSeconds": 900, "hint": "Vérifiez le câble de l'imprimante."}],
         "summary": {"ok": 1, "pending": 0, "down": 1, "absent": 0, "allOk": false, "label": "1 appareil à vérifier"}, "thresholdSeconds": 300, "exercise": false, "screen": "pertes.boitiers", "checkedAt": "2026-11-16T11:15:45+00:00"}
        """;

    // admin-boitiers.json (corps, abrégé)
    private const string BoxesJson = """
        {"items": [{"id": 1, "name": "Boîtier de la caisse 1", "place": "Près de la caisse", "status": "active", "statusLabel": "Inscrit", "online": true, "onlineLabel": "En ligne", "lastSeenLabel": "16/11/2026 06 h 15", "keyHint": "IZ-BOX-XX**-****-****", "exercise": false, "paired": false,
                    "devices": [{"id": 1, "serial": "LECTEUR-0001", "kind": "reader", "kindLabel": "Lecteur de puces", "status": "ok", "statusLabel": "En marche"},
                                {"id": 2, "serial": "IMPR-0001", "kind": "printer", "kindLabel": "Imprimante à puce", "status": "down", "statusLabel": "En panne ou débranché"}]}],
         "total": 1, "limit": 20, "exercise": false, "screen": "pertes.boitiers"}
        """;

    // admin-boitier-jumele.json (corps, abrégé)
    private const string PairedJson = """
        {"box": {"id": 2, "name": "Boîtier de la réserve", "place": "Réserve", "status": "pending", "statusLabel": "En attente de sa machine", "online": false, "onlineLabel": "—", "keyHint": null, "exercise": false, "paired": true, "devices": []}}
        """;

    private const string NeutralRefusal = "Code inconnu, ou boîtier pas encore branché à Internet. Branchez le boîtier, attendez que son voyant clignote, puis réessayez.";

    [Theory]
    [InlineData("kx7 42p 9qm", "KX7-42P-9QM")]
    [InlineData("KX7-42P-9QM", "KX7-42P-9QM")]
    [InlineData("kx742p9qm", "KX7-42P-9QM")]
    [InlineData("https://plateforme.example/jumeler?code=KX7-42P-9QM&v=1", "KX7-42P-9QM")]
    [InlineData("KX7-42P", null)]
    [InlineData("KX7-42P-9QMX", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void PairingCode_IsNormalized(string? raw, string? expected)
    {
        Assert.Equal(expected, LossPairingCode.Normalize(raw));
    }

    [Fact]
    public void Fixtures_AreRead_WithoutAnyKey()
    {
        var boxes = JsonConvert.DeserializeObject<LossBoxList>(BoxesJson)!;
        var box = Assert.Single(boxes.items);
        Assert.Equal("Inscrit · En ligne", box.StateText);
        Assert.Equal("Lecteur de puces : En marche\nImprimante à puce : En panne ou débranché", box.DevicesText);
        // Le modèle mobile ne porte aucune clé ni indice de clé.
        Assert.Null(typeof(LossBox).GetProperty("keyHint"));

        var paired = JsonConvert.DeserializeObject<LossBoxPairResult>(PairedJson)!.box!;
        Assert.Equal("En attente de sa machine", paired.StateText);
        Assert.Equal("Aucun appareil branché", paired.DevicesText);
    }

    [Fact]
    public async Task Health_IsRead_WithoutRefreshingTheSession()
    {
        var http = new FakeHttpService { ResultToReturn = JsonConvert.DeserializeObject<LossHealth>(HealthJson) };
        var service = new LossPreventionService(http);

        var health = await service.GetHealthAsync();

        Assert.Single(http.RequestedWithoutRefresh);
        Assert.StartsWith("gestion/pertes/sante", http.RequestedWithoutRefresh[0]);
        Assert.Empty(http.RequestedUrls);
        Assert.Equal(LossHealthDevice.DownColor, health.devices[1].DotColor);
        Assert.Equal(LossHealthDevice.DownColor, health.summary.BannerColor);
    }

    [Fact]
    public async Task Start_ShowsDevicesAndBoxes_ThenPollsEveryFiveSeconds()
    {
        var service = new FakeService();
        var (vm, delays) = Create(service);

        await vm.StartAsync();
        await delays.Tick();
        await delays.Tick();

        Assert.Equal(2, vm.Devices.Count);
        Assert.Equal("1 appareil à vérifier", vm.Summary!.label);
        Assert.Single(vm.Boxes);
        Assert.True(vm.HasBoxes);
        Assert.Equal(3, service.HealthCalls);
        Assert.All(delays.Waits, wait => Assert.Equal(LossHardwareViewModel.PollInterval, wait));

        vm.Stop();
        await delays.Tick();
        Assert.Equal(3, service.HealthCalls);
    }

    [Fact]
    public async Task ExpiredSession_StopsPolling_UntilResume()
    {
        var service = new FakeService();
        var (vm, delays) = Create(service);
        await vm.StartAsync();

        service.HealthError = new ApiException(new ApiError { Code = "Unauthorized", Message = "Session expirée." }, HttpStatusCode.Unauthorized);
        await delays.Tick();

        Assert.True(vm.IsPollingPaused);
        var callsWhenPaused = service.HealthCalls;
        await delays.Tick();
        Assert.Equal(callsWhenPaused, service.HealthCalls);

        service.HealthError = null;
        await vm.ResumeCommand.ExecuteAsync(null);
        Assert.False(vm.IsPollingPaused);
        Assert.Equal(callsWhenPaused + 1, service.HealthCalls);
        Assert.Equal(2, service.BoxesCalls);
    }

    [Fact]
    public async Task InactiveModule_ShowsTheNotice_AndNeverPolls()
    {
        var service = new FakeService { BoxesError = new ApiException(new ApiError { Code = ModuleInactiveInfo.Code, Message = "Module inactif." }, HttpStatusCode.Forbidden) };
        var (vm, delays) = Create(service);

        await vm.StartAsync();

        Assert.True(vm.IsModuleInactive);
        Assert.False(vm.CanPair);
        Assert.Equal(0, service.HealthCalls);
        Assert.Empty(delays.Waits);
    }

    [Fact]
    public async Task ScannedQr_FillsTheCode()
    {
        var scanner = new FakeScanner { Next = "https://plateforme.example/jumeler?code=kx7-42p-9qm" };
        var (vm, _) = Create(new FakeService(), scanner);

        await vm.ScanCodeCommand.ExecuteAsync(null);

        Assert.Equal("KX7-42P-9QM", vm.PairCode);
        Assert.False(vm.HasPairMessage);
    }

    [Fact]
    public async Task ScannedQr_ThatIsNotABoxCode_IsRefused()
    {
        var scanner = new FakeScanner { Next = "https://exemple.org/produit/42" };
        var (vm, _) = Create(new FakeService(), scanner);

        await vm.ScanCodeCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, vm.PairCode);
        Assert.Contains("n'est pas celui d'un boîtier", vm.PairMessage);
    }

    [Fact]
    public async Task Pair_SendsTheNormalizedCode_AndReloads()
    {
        var service = new FakeService();
        var (vm, _) = Create(service);
        vm.PairCode = "kx7 42p 9qm";
        vm.PairName = "  Boîtier de la réserve ";
        vm.PairPlace = "Réserve";

        await vm.PairCommand.ExecuteAsync(null);

        Assert.Equal(("KX7-42P-9QM", "Boîtier de la réserve", "Réserve"), service.Pairings.Single());
        Assert.Contains("« Boîtier de la réserve » est ajouté", vm.PairMessage);
        Assert.Equal(string.Empty, vm.PairCode);
        Assert.Equal(1, service.BoxesCalls);
        Assert.Equal(1, service.HealthCalls);
    }

    [Fact]
    public async Task Pair_WithABadCodeOrNoName_SendsNothing()
    {
        var service = new FakeService();
        var (vm, _) = Create(service);

        vm.PairCode = "KX7";
        vm.PairName = "Réserve";
        await vm.PairCommand.ExecuteAsync(null);
        Assert.Contains("9 caractères", vm.PairMessage);

        vm.PairCode = "KX7-42P-9QM";
        vm.PairName = " ";
        await vm.PairCommand.ExecuteAsync(null);
        Assert.Contains("Donnez un nom", vm.PairMessage);

        Assert.Empty(service.Pairings);
    }

    [Fact]
    public async Task Pair_RefusedCode_ShowsTheNeutralServerText_AndRereadsTheList()
    {
        var service = new FakeService
        {
            PairError = new ApiException(new ApiError { Code = "PAIRING_CODE_INVALID", Message = NeutralRefusal }, HttpStatusCode.NotFound),
        };
        var dialog = new FakeDialogService();
        var (vm, _) = Create(service, dialog: dialog);
        vm.PairCode = "ABC-DEF-GHJ";
        vm.PairName = "Boîtier de la réserve";

        await vm.PairCommand.ExecuteAsync(null);

        Assert.Equal(NeutralRefusal, vm.PairMessage);
        Assert.Equal("ABC-DEF-GHJ", vm.PairCode);
        Assert.Equal(1, service.BoxesCalls);
        Assert.Empty(dialog.ShownErrors);
        Assert.True(vm.IsPairIdle);
    }

    [Fact]
    public async Task Pair_TooManyTries_SaysWhenToRetry()
    {
        var service = new FakeService
        {
            PairError = new ApiException(new ApiError { Code = "RATE_LIMITED", Message = "Trop d'essais.", RetryAfterSeconds = 40 }, (HttpStatusCode)429),
        };
        var (vm, _) = Create(service);
        vm.PairCode = "KX7-42P-9QM";
        vm.PairName = "Réserve";

        await vm.PairCommand.ExecuteAsync(null);

        Assert.Equal("Trop d'essais. Réessayez dans 40 s.", vm.PairMessage);
    }

    // --- Outils ---

    private static (LossHardwareViewModel, ManualDelays) Create(FakeService service, FakeScanner? scanner = null, FakeDialogService? dialog = null)
    {
        var delays = new ManualDelays();
        var vm = new LossHardwareViewModel(service, dialog ?? new FakeDialogService(), scanner ?? new FakeScanner())
        {
            DelayAsync = delays.WaitAsync,
        };
        return (vm, delays);
    }

    /// <summary>Attentes pilotées par le test : chaque Tick libère l'attente en cours et laisse la lecture suivante se faire.</summary>
    private sealed class ManualDelays
    {
        private TaskCompletionSource? _pending;
        public List<TimeSpan> Waits { get; } = new();

        public Task WaitAsync(TimeSpan delay, CancellationToken token)
        {
            Waits.Add(delay);
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            token.Register(() => tcs.TrySetCanceled(token));
            _pending = tcs;
            return tcs.Task;
        }

        public async Task Tick()
        {
            var pending = _pending;
            _pending = null;
            pending?.TrySetResult();
            for (var i = 0; i < 20; i++)
                await Task.Yield();
            await Task.Delay(20);
        }
    }

    private sealed class FakeScanner : IBarcodeScanner
    {
        public string? Next { get; set; }
        public Task<string?> ScanAsync() => Task.FromResult(Next);
        public Task<string?> ScanQrAsync() => Task.FromResult(Next);
    }

    private sealed class FakeService : ILossPreventionService
    {
        public int HealthCalls { get; private set; }
        public int BoxesCalls { get; private set; }
        public Exception? HealthError { get; set; }
        public Exception? BoxesError { get; set; }
        public Exception? PairError { get; set; }
        public List<(string Code, string Name, string? Place)> Pairings { get; } = new();

        public Task<LossHealth> GetHealthAsync()
        {
            HealthCalls++;
            return HealthError != null
                ? Task.FromException<LossHealth>(HealthError)
                : Task.FromResult(JsonConvert.DeserializeObject<LossHealth>(HealthJson)!);
        }

        public Task<LossBoxList> GetBoxesAsync()
        {
            BoxesCalls++;
            return BoxesError != null
                ? Task.FromException<LossBoxList>(BoxesError)
                : Task.FromResult(JsonConvert.DeserializeObject<LossBoxList>(BoxesJson)!);
        }

        public Task<LossBoxPairResult> PairBoxAsync(string code, string name, string? place)
        {
            if (PairError != null)
                return Task.FromException<LossBoxPairResult>(PairError);
            Pairings.Add((code, name, place));
            return Task.FromResult(JsonConvert.DeserializeObject<LossBoxPairResult>(PairedJson)!);
        }

        public Task<LossSummary> GetSummaryAsync(bool analyse = true) => Task.FromResult(new LossSummary());
        public Task<List<LossFinding>> GetFindingsAsync(string? status, bool onlyNew, int page, int limit) => Task.FromResult(new List<LossFinding>());
        public Task<LossFindingDetail> GetFindingAsync(int id) => Task.FromResult(new LossFindingDetail());
        public Task<LossFindingDetail> DecideAsync(int id, string status, string? note) => Task.FromResult(new LossFindingDetail());
        public Task<LossExercises> GetExercisesAsync() => Task.FromResult(new LossExercises());
        public Task<LossExerciseLaunch> LaunchExerciseAsync(string scenario, bool sendTestEmail) => Task.FromResult(new LossExerciseLaunch());
        public Task<LossFindingDetail> GetExerciseAsync(int id) => Task.FromResult(new LossFindingDetail());
        public Task<LossFindingDetail> DecideExerciseAsync(int id, string status, string? note) => Task.FromResult(new LossFindingDetail());
        public Task DeleteExercisesAsync() => Task.CompletedTask;
    }
}
