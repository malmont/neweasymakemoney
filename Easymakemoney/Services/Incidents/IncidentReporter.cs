using System.Text.Json;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Auth;
using Easymakemoney.Services.Tenant;

namespace Easymakemoney.Services.Incidents;

public interface IIncidentReporter
{
    void ReportException(Exception exception, string level = IncidentLevel.Error);
    void ReportNetworkFailure(Uri? requestUri, string method, long durationMs);
    /// <summary>Plantage : écrit le rapport en local, sans réseau, pour l'envoyer au lancement suivant.</summary>
    void WriteCrashMarker(Exception exception);
    Task StartAsync();
    Task FlushAsync();
}

/// <summary>
/// Remontée des incidents de l'application de gestion (lot INC-2) : erreurs non attrapées, plantages relus au lancement suivant,
/// erreurs réseau répétées. Jamais les réponses 4xx ou 5xx du backend (le serveur enregistre déjà ses 5xx), jamais hors d'une
/// version publiée. L'envoi ne bloque rien, n'affiche rien et ne relance aucune erreur.
/// </summary>
public class IncidentReporter : IIncidentReporter
{
#if DEBUG
    public static bool Enabled { get; set; } = false;
#else
    public static bool Enabled { get; set; } = true;
#endif

    public static readonly TimeSpan SendSpacing = TimeSpan.FromSeconds(2);

    private readonly IIncidentSender _sender;
    private readonly IIncidentQueue _queue;
    private readonly IIncidentEnvironment _environment;
    private readonly IncidentThrottle _throttle;
    private readonly NetworkFailureTracker _network;
    private readonly Func<DateTime> _now;
    private readonly SemaphoreSlim _flushing = new(1, 1);
    private readonly object _queueLock = new();
    private DateTime _pausedUntil = DateTime.MinValue;

    public IncidentReporter(IIncidentSender sender, IIncidentQueue queue, IIncidentEnvironment environment, Func<DateTime>? now = null)
    {
        _sender = sender;
        _queue = queue;
        _environment = environment;
        _now = now ?? (() => DateTime.UtcNow);
        _throttle = new IncidentThrottle(_now);
        _network = new NetworkFailureTracker(_now);
    }

    /// <summary>Attente entre deux envois (remplacée dans les tests).</summary>
    public Func<TimeSpan, Task> DelayAsync { get; set; } = Task.Delay;

    /// <summary>Chemin du marqueur de plantage (remplacé dans les tests).</summary>
    public string CrashMarkerPath { get; set; } = DefaultCrashMarkerPath();

    /// <summary>Une réponse du backend, une annulation ou une session finie ne sont pas des pannes de l'application.</summary>
    public static bool IsReportable(Exception exception) => exception switch
    {
        ApiException => false,
        OperationCanceledException => false,
        SessionExpiredException => false,
        AuthenticationMissingException => false,
        TenantMissingException => false,
        HttpRequestException => false,
        AggregateException aggregate => aggregate.InnerExceptions.Any(IsReportable),
        _ => true,
    };

    public void ReportException(Exception exception, string level = IncidentLevel.Error)
    {
        try
        {
            if (!Enabled || !IsReportable(exception))
                return;
            var root = exception is AggregateException aggregate ? aggregate.InnerExceptions.First(IsReportable) : exception;
            Enqueue(Build(root, level, route: null, extra: null));
        }
        catch (Exception)
        {
            // Jamais d'erreur à cause de la remontée des erreurs.
        }
    }

    public void ReportNetworkFailure(Uri? requestUri, string method, long durationMs)
    {
        try
        {
            if (!Enabled || !_environment.IsOnline)
                return;
            var route = IncidentSanitizer.Route(requestUri);
            if (route == null || route.Contains("incidents/report", StringComparison.Ordinal) || !_network.Record($"{method} {route}"))
                return;
            var report = new IncidentReport
            {
                level = IncidentLevel.Warning,
                route = route,
                message = "Network failure: no response from the server",
            };
            Fill(report);
            report.context["errorCode"] = "NETWORK";
            report.context["httpStatus"] = 0;
            report.context["method"] = method;
            report.context["durationMs"] = durationMs;
            Enqueue(report);
        }
        catch (Exception)
        {
        }
    }

    public void WriteCrashMarker(Exception exception)
    {
        try
        {
            if (!Enabled || !IsReportable(exception))
                return;
            var report = Build(exception, IncidentLevel.Crash, route: null, extra: null);
            File.WriteAllText(CrashMarkerPath, IncidentSanitizer.Serialize(report));
        }
        catch (Exception)
        {
        }
    }

    /// <summary>Au lancement : le plantage noté la dernière fois rejoint la file, puis la file part.</summary>
    public async Task StartAsync()
    {
        try
        {
            if (!Enabled)
                return;
            if (File.Exists(CrashMarkerPath))
            {
                var body = File.ReadAllText(CrashMarkerPath);
                File.Delete(CrashMarkerPath);
                if (!string.IsNullOrWhiteSpace(body))
                    AddToQueue(body);
            }
            await FlushAsync();
        }
        catch (Exception)
        {
        }
    }

    /// <summary>Envoie la file, un rapport toutes les 2 s, tant que le réseau répond ; un 429 suspend jusqu'à Retry-After.</summary>
    public async Task FlushAsync()
    {
        if (!Enabled || !await _flushing.WaitAsync(0))
            return;
        try
        {
            var first = true;
            while (_environment.IsOnline && _now() >= _pausedUntil)
            {
                QueuedIncident? next;
                lock (_queueLock)
                    next = IncidentQueueRules.Trim(_queue.Load(), _now()).FirstOrDefault();
                if (next == null)
                    return;
                if (!first)
                    await DelayAsync(SendSpacing);
                first = false;

                var (outcome, retryAfter) = await _sender.SendAsync(next.Body);
                switch (outcome)
                {
                    case SendOutcome.Accepted:
                    case SendOutcome.Dropped:
                        Remove(next);
                        break;
                    case SendOutcome.RetryLater:
                        _pausedUntil = _now() + (retryAfter ?? TimeSpan.FromSeconds(60));
                        return;
                    default:
                        return;
                }
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            _flushing.Release();
        }
    }

    private IncidentReport Build(Exception exception, string level, string? route, Dictionary<string, object>? extra)
    {
        var report = new IncidentReport
        {
            level = level,
            route = route,
            message = IncidentSanitizer.Message(exception),
        };
        Fill(report);
        if (IncidentSanitizer.Stack(exception) is { } stack)
            report.context["stack"] = stack;
        if (extra != null)
            foreach (var pair in extra)
                report.context[pair.Key] = pair.Value;
        return report;
    }

    private void Fill(IncidentReport report)
    {
        report.appVersion = _environment.AppVersion;
        report.screen = IncidentSanitizer.Screen(_environment.CurrentLocation);
        Put(report, "os", _environment.Os);
        Put(report, "osVersion", _environment.OsVersion);
        Put(report, "device", _environment.Device);
        Put(report, "locale", _environment.Locale);
        report.context["online"] = _environment.IsOnline;
    }

    private static void Put(IncidentReport report, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            report.context[key] = value;
    }

    private void Enqueue(IncidentReport report)
    {
        if (_throttle.Admit(IncidentSanitizer.Fingerprint(report)) is not { } repeats)
            return;
        if (repeats > 1)
            report.context["repeats"] = repeats;
        AddToQueue(IncidentSanitizer.Serialize(report));
        _ = FlushAsync();
    }

    private void AddToQueue(string body)
    {
        lock (_queueLock)
        {
            var items = _queue.Load().ToList();
            items.Add(new QueuedIncident { Body = body, QueuedAt = _now() });
            _queue.Save(IncidentQueueRules.Trim(items, _now()));
        }
    }

    private void Remove(QueuedIncident sent)
    {
        lock (_queueLock)
        {
            var items = _queue.Load().Where(item => !(item.QueuedAt == sent.QueuedAt && item.Body == sent.Body)).ToList();
            _queue.Save(items);
        }
    }

    private static string DefaultCrashMarkerPath()
    {
        try
        {
            return Path.Combine(FileSystem.Current.AppDataDirectory, "incident-crash.json");
        }
        catch (Exception)
        {
            return Path.Combine(Path.GetTempPath(), "incident-crash.json");
        }
    }
}

/// <summary>
/// Repère les appels au backend restés sans réponse (délai dépassé, connexion refusée) pour la règle « 3 échecs en 5 minutes ».
/// Les réponses, quel que soit leur statut, ne sont jamais comptées.
/// </summary>
public class IncidentNetworkHandler : DelegatingHandler
{
    private readonly IIncidentReporter _reporter;

    public IncidentNetworkHandler(IIncidentReporter reporter)
    {
        _reporter = reporter;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException or IOException
                                   || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            _reporter.ReportNetworkFailure(request.RequestUri, request.Method.Method, started.ElapsedMilliseconds);
            throw;
        }
    }
}
