using System.Net;
using System.Text.Json;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Incidents;
using Xunit;

namespace Easymakemoney.Tests.Services;

[Collection(IncidentCollection.Name)]
public class IncidentTests : IDisposable
{
    private readonly bool _enabled = IncidentReporter.Enabled;
    private readonly string _marker = Path.Combine(Path.GetTempPath(), $"incident-{Guid.NewGuid():N}.json");

    public IncidentTests() => IncidentReporter.Enabled = true;

    public void Dispose()
    {
        IncidentReporter.Enabled = _enabled;
        if (File.Exists(_marker))
            File.Delete(_marker);
    }

    private sealed class FakeEnvironment : IIncidentEnvironment
    {
        public string? AppVersion => "2.1.0+57";
        public string? Os => "android";
        public string? OsVersion => "14";
        public string? Device => "SM-A546B";
        public string? Locale => "fr-CA";
        public bool IsOnline { get; set; } = true;
        public string? CurrentLocation { get; set; } = "//accueil/LossFindingPage?id=42";
    }

    private sealed class FakeSender : IIncidentSender
    {
        public List<string> Sent { get; } = new();
        public Queue<(SendOutcome, TimeSpan?)> Outcomes { get; } = new();

        public Task<(SendOutcome Outcome, TimeSpan? RetryAfter)> SendAsync(string body)
        {
            Sent.Add(body);
            return Task.FromResult(Outcomes.Count > 0 ? Outcomes.Dequeue() : (SendOutcome.Accepted, (TimeSpan?)null));
        }
    }

    private static Exception Thrown(Exception ex)
    {
        try { throw ex; } catch (Exception caught) { return caught; }
    }

    private (IncidentReporter Reporter, FakeSender Sender, MemoryIncidentQueue Queue, FakeEnvironment Env, Func<DateTime> Clock, Action<TimeSpan> Advance) Create()
    {
        var now = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
        var sender = new FakeSender();
        var queue = new MemoryIncidentQueue();
        var env = new FakeEnvironment();
        var reporter = new IncidentReporter(sender, queue, env, () => now)
        {
            DelayAsync = _ => Task.CompletedTask,
            CrashMarkerPath = _marker,
        };
        return (reporter, sender, queue, env, () => now, span => now += span);
    }

    private static JsonElement Parse(string body) => JsonDocument.Parse(body).RootElement;

    [Fact]
    public void Le_message_qui_recopie_une_valeur_est_reduit_a_son_type()
    {
        Assert.Equal("System.FormatException", IncidentSanitizer.Message(new FormatException("Invalid email: marie@exemple.com")));
        Assert.Equal("System.ArgumentException", IncidentSanitizer.Message(new ArgumentException("Carte 4242424242424242 refusée")));
        Assert.Equal("System.InvalidOperationException", IncidentSanitizer.Message(new InvalidOperationException("Client \"Dupont\" introuvable")));
        Assert.Equal("System.NullReferenceException: Object reference not set to an instance of an object.",
            IncidentSanitizer.Message(new NullReferenceException("Object reference not set to an instance of an object.")));
    }

    [Fact]
    public void L_ecran_et_la_route_n_ont_ni_identifiant_ni_requete()
    {
        Assert.Equal("accueil/LossFindingPage", IncidentSanitizer.Screen("//accueil/LossFindingPage?id=42#haut"));
        Assert.Equal("gestion/commandes/{id}", IncidentSanitizer.Screen("//gestion/commandes/125"));
        Assert.Equal("/api/orders/{id}/lines/{id}", IncidentSanitizer.Route(new Uri("https://exemple.test/api/orders/4521/lines/7?page=2")));
        Assert.Equal("/api/files/{id}", IncidentSanitizer.Route(new Uri("https://exemple.test/api/files/0f8fad5b-d9cb-469f-a165-70867728950e")));
    }

    [Fact]
    public void La_pile_garde_dix_lignes_sans_dossier()
    {
        var ex = Thrown(new InvalidOperationException("boom"));
        var stack = IncidentSanitizer.Stack(ex)!;
        Assert.StartsWith("at ", stack);
        Assert.True(stack.Split('\n').Length <= IncidentSanitizer.MaxStackLines);
        Assert.DoesNotContain("/Users/", stack);
        Assert.DoesNotContain("\\", stack);
    }

    [Fact]
    public void Le_corps_tient_dans_quatre_ko_en_retirant_la_pile_d_abord()
    {
        var report = new IncidentReport { message = "x", context = { ["stack"] = new string('a', 6000) } };
        var body = IncidentSanitizer.Serialize(report);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(body) <= IncidentSanitizer.MaxBytes);
        Assert.False(Parse(body).GetProperty("context").TryGetProperty("stack", out _));
    }

    [Fact]
    public void Un_seul_envoi_par_minute_puis_les_repetitions_dans_repeats()
    {
        var now = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
        var throttle = new IncidentThrottle(() => now);
        Assert.Equal(1, throttle.Admit("a"));
        Assert.Null(throttle.Admit("a"));
        Assert.Null(throttle.Admit("a"));
        Assert.Equal(1, throttle.Admit("b"));
        now = now.AddSeconds(61);
        Assert.Equal(3, throttle.Admit("a"));
    }

    [Fact]
    public void Trente_rapports_au_plus_par_lancement()
    {
        var throttle = new IncidentThrottle();
        for (var i = 0; i < IncidentThrottle.SessionCap; i++)
            Assert.NotNull(throttle.Admit($"f{i}"));
        Assert.Null(throttle.Admit("encore"));
    }

    [Fact]
    public void Trois_echecs_en_cinq_minutes_sur_la_meme_route()
    {
        var now = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
        var tracker = new NetworkFailureTracker(() => now);
        Assert.False(tracker.Record("GET /api/x"));
        now = now.AddMinutes(4);
        Assert.False(tracker.Record("GET /api/x"));
        Assert.False(tracker.Record("GET /api/y"));
        Assert.True(tracker.Record("GET /api/x"));
        Assert.False(tracker.Record("GET /api/x"));
        now = now.AddMinutes(6);
        Assert.False(tracker.Record("GET /api/x"));
    }

    [Fact]
    public void Les_reponses_du_backend_et_les_annulations_ne_sont_pas_des_pannes()
    {
        Assert.False(IncidentReporter.IsReportable(new ApiException(new ApiError { Code = "X" }, HttpStatusCode.InternalServerError)));
        Assert.False(IncidentReporter.IsReportable(new OperationCanceledException()));
        Assert.False(IncidentReporter.IsReportable(new HttpRequestException()));
        Assert.True(IncidentReporter.IsReportable(new NullReferenceException()));
        Assert.True(IncidentReporter.IsReportable(new AggregateException(new TaskCanceledException(), new InvalidOperationException())));
    }

    [Fact]
    public async Task Une_erreur_part_avec_le_contrat_INC2()
    {
        var (reporter, sender, queue, _, _, _) = Create();
        reporter.ReportException(Thrown(new NullReferenceException("Object reference not set to an instance of an object.")));
        await reporter.FlushAsync();

        var body = Assert.Single(sender.Sent);
        var json = Parse(body);
        Assert.Equal("gestion", json.GetProperty("app").GetString());
        Assert.Equal("error", json.GetProperty("level").GetString());
        Assert.Equal("accueil/LossFindingPage", json.GetProperty("screen").GetString());
        Assert.Equal("2.1.0+57", json.GetProperty("appVersion").GetString());
        var context = json.GetProperty("context");
        Assert.Equal("SM-A546B", context.GetProperty("device").GetString());
        Assert.Equal("android", context.GetProperty("os").GetString());
        var allowed = new HashSet<string> { "url", "method", "httpStatus", "errorCode", "component", "action", "os", "osVersion", "device", "locale", "online", "durationMs", "stack", "repeats" };
        foreach (var property in context.EnumerateObject())
            Assert.Contains(property.Name, allowed);
        Assert.Empty(queue.Load());
    }

    [Fact]
    public async Task Une_reponse_du_backend_n_est_jamais_rapportee()
    {
        var (reporter, sender, _, _, _, _) = Create();
        reporter.ReportException(new ApiException(new ApiError { Code = "VALIDATION_FAILED" }, HttpStatusCode.UnprocessableEntity));
        await reporter.FlushAsync();
        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task Hors_connexion_le_rapport_attend_puis_part_au_retour()
    {
        var (reporter, sender, queue, env, _, _) = Create();
        env.IsOnline = false;
        reporter.ReportException(Thrown(new InvalidOperationException("boom")));
        await reporter.FlushAsync();
        Assert.Empty(sender.Sent);
        Assert.Single(queue.Load());

        env.IsOnline = true;
        await reporter.FlushAsync();
        Assert.Single(sender.Sent);
        Assert.Empty(queue.Load());
    }

    [Fact]
    public async Task Panne_garde_le_rapport_422_le_jette_429_suspend()
    {
        var (reporter, sender, queue, _, _, advance) = Create();
        sender.Outcomes.Enqueue((SendOutcome.Keep, null));
        reporter.ReportException(Thrown(new InvalidOperationException("un"))); // l'envoi part aussitôt et échoue : le rapport reste
        Assert.Single(sender.Sent);
        Assert.Single(queue.Load());

        sender.Outcomes.Enqueue((SendOutcome.RetryLater, TimeSpan.FromSeconds(30)));
        await reporter.FlushAsync();
        Assert.Single(queue.Load());
        var sentBefore = sender.Sent.Count;
        await reporter.FlushAsync();
        Assert.Equal(sentBefore, sender.Sent.Count);

        advance(TimeSpan.FromSeconds(31));
        sender.Outcomes.Enqueue((SendOutcome.Dropped, null));
        await reporter.FlushAsync();
        Assert.Empty(queue.Load());
    }

    [Fact]
    public void La_file_garde_vingt_rapports_de_moins_de_24_heures()
    {
        var now = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
        var items = Enumerable.Range(0, 25).Select(i => new QueuedIncident { Body = $"{i}", QueuedAt = now.AddMinutes(-i) })
            .Append(new QueuedIncident { Body = "vieux", QueuedAt = now.AddHours(-25) });
        var kept = IncidentQueueRules.Trim(items, now);
        Assert.Equal(IncidentQueueRules.MaxItems, kept.Count);
        Assert.DoesNotContain(kept, item => item.Body == "vieux");
        Assert.Contains(kept, item => item.Body == "0");
        Assert.DoesNotContain(kept, item => item.Body == "24");
    }

    [Fact]
    public async Task Le_plantage_part_au_lancement_suivant_une_seule_fois()
    {
        var (reporter, sender, _, _, _, _) = Create();
        reporter.WriteCrashMarker(Thrown(new InvalidOperationException("boom")));
        Assert.True(File.Exists(_marker));

        await reporter.StartAsync();
        Assert.False(File.Exists(_marker));
        var json = Parse(Assert.Single(sender.Sent));
        Assert.Equal("crash", json.GetProperty("level").GetString());

        await reporter.StartAsync();
        Assert.Single(sender.Sent);
    }

    [Fact]
    public async Task Le_reseau_repete_donne_un_warning_NETWORK()
    {
        var (reporter, sender, _, _, _, _) = Create();
        var uri = new Uri("https://exemple.test/api/gestion/commandes/12");
        reporter.ReportNetworkFailure(uri, "GET", 30000);
        reporter.ReportNetworkFailure(uri, "GET", 30000);
        await reporter.FlushAsync();
        Assert.Empty(sender.Sent);

        reporter.ReportNetworkFailure(uri, "GET", 30000);
        await reporter.FlushAsync();
        var json = Parse(Assert.Single(sender.Sent));
        Assert.Equal("warning", json.GetProperty("level").GetString());
        Assert.Equal("/api/gestion/commandes/{id}", json.GetProperty("route").GetString());
        Assert.Equal("NETWORK", json.GetProperty("context").GetProperty("errorCode").GetString());
        Assert.Equal(0, json.GetProperty("context").GetProperty("httpStatus").GetInt32());
    }

    [Fact]
    public async Task Desactive_hors_version_publiee_rien_ne_part()
    {
        var (reporter, sender, queue, _, _, _) = Create();
        IncidentReporter.Enabled = false;
        reporter.ReportException(Thrown(new InvalidOperationException("boom")));
        reporter.WriteCrashMarker(Thrown(new InvalidOperationException("boom")));
        await reporter.FlushAsync();
        Assert.Empty(sender.Sent);
        Assert.Empty(queue.Load());
        Assert.False(File.Exists(_marker));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        public Exception? Error { get; set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Error != null)
                throw Error;
            return Task.FromResult(new HttpResponseMessage(Status));
        }
    }

    private sealed class RecordingReporter : IIncidentReporter
    {
        public int NetworkFailures { get; private set; }
        public void ReportException(Exception exception, string level = IncidentLevel.Error) { }
        public void ReportNetworkFailure(Uri? requestUri, string method, long durationMs) => NetworkFailures++;
        public void WriteCrashMarker(Exception exception) { }
        public Task StartAsync() => Task.CompletedTask;
        public Task FlushAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task Le_handler_ne_compte_que_les_appels_sans_reponse()
    {
        var recorder = new RecordingReporter();
        var inner = new ThrowingHandler { Status = HttpStatusCode.InternalServerError };
        var client = new HttpClient(new IncidentNetworkHandler(recorder) { InnerHandler = inner });

        using (await client.GetAsync("https://exemple.test/api/x")) { }
        Assert.Equal(0, recorder.NetworkFailures);

        inner.Error = new HttpRequestException("refused");
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("https://exemple.test/api/x"));
        Assert.Equal(1, recorder.NetworkFailures);

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        inner.Error = new TaskCanceledException();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync("https://exemple.test/api/x", cancelled.Token));
        Assert.Equal(1, recorder.NetworkFailures);
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public class IncidentCollection
{
    public const string Name = "Incidents";
}
