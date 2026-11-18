namespace Easymakemoney.Services.Incidents;

/// <summary>
/// Regroupement côté client (INC-2 § 6) : au plus un envoi par empreinte et par minute, les répétitions de la minute
/// comptées dans context.repeats du prochain envoi ; au plus 30 rapports par lancement de l'application.
/// </summary>
public class IncidentThrottle
{
    public const int SessionCap = 30;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly Func<DateTime> _now;
    private readonly Dictionary<string, (DateTime SentAt, int Held)> _seen = new();
    private int _sent;

    public IncidentThrottle(Func<DateTime>? now = null)
    {
        _now = now ?? (() => DateTime.UtcNow);
    }

    /// <summary>Le nombre à mettre dans repeats si le rapport part, ou null s'il est retenu.</summary>
    public int? Admit(string fingerprint)
    {
        lock (_seen)
        {
            if (_sent >= SessionCap)
                return null;
            var now = _now();
            if (_seen.TryGetValue(fingerprint, out var entry) && now - entry.SentAt < Window)
            {
                _seen[fingerprint] = (entry.SentAt, Math.Min(entry.Held + 1, 999));
                return null;
            }
            var repeats = 1 + (entry.Held > 0 ? entry.Held : 0);
            _seen[fingerprint] = (now, 0);
            _sent++;
            return repeats;
        }
    }
}

/// <summary>
/// Erreur réseau répétée (INC-2 § 4) : le même appel échoue 3 fois en 5 minutes alors que l'appareil se dit en ligne.
/// Un seul rapport, puis le compte repart de zéro.
/// </summary>
public class NetworkFailureTracker
{
    public const int Threshold = 3;
    public static readonly TimeSpan Span = TimeSpan.FromMinutes(5);

    private readonly Func<DateTime> _now;
    private readonly Dictionary<string, List<DateTime>> _failures = new();

    public NetworkFailureTracker(Func<DateTime>? now = null)
    {
        _now = now ?? (() => DateTime.UtcNow);
    }

    /// <summary>Vrai quand cet échec est le 3e en 5 minutes sur cette route : il faut alors envoyer un rapport.</summary>
    public bool Record(string route)
    {
        lock (_failures)
        {
            var now = _now();
            if (!_failures.TryGetValue(route, out var times))
                _failures[route] = times = new List<DateTime>();
            times.RemoveAll(at => now - at > Span);
            times.Add(now);
            if (times.Count < Threshold)
                return false;
            times.Clear();
            return true;
        }
    }
}
