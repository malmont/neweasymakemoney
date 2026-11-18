using System.Text.Json;

namespace Easymakemoney.Services.Incidents;

/// <summary>Rapports en attente d'envoi (hors connexion, serveur indisponible) : 20 au plus, 24 h au plus, les plus anciens jetés.</summary>
public interface IIncidentQueue
{
    IReadOnlyList<QueuedIncident> Load();
    void Save(IReadOnlyList<QueuedIncident> items);
}

public class QueuedIncident
{
    public string Body { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
}

public static class IncidentQueueRules
{
    public const int MaxItems = 20;
    public static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

    public static List<QueuedIncident> Trim(IEnumerable<QueuedIncident> items, DateTime now) =>
        items.Where(item => now - item.QueuedAt <= MaxAge)
            .OrderBy(item => item.QueuedAt)
            .TakeLast(MaxItems)
            .ToList();
}

public class MemoryIncidentQueue : IIncidentQueue
{
    private List<QueuedIncident> _items = new();
    public IReadOnlyList<QueuedIncident> Load() => _items.ToList();
    public void Save(IReadOnlyList<QueuedIncident> items) => _items = items.ToList();
}

/// <summary>Fichier dans le dossier privé de l'application. Vidé au changement de site comme le reste (SiteDataCleaner n'y touche pas : aucun contenu de site).</summary>
public class FileIncidentQueue : IIncidentQueue
{
    private static string PathOf => System.IO.Path.Combine(FileSystem.Current.AppDataDirectory, "incidents-queue.json");

    public IReadOnlyList<QueuedIncident> Load()
    {
        try
        {
            return File.Exists(PathOf)
                ? System.Text.Json.JsonSerializer.Deserialize<List<QueuedIncident>>(File.ReadAllText(PathOf)) ?? new List<QueuedIncident>()
                : new List<QueuedIncident>();
        }
        catch (Exception)
        {
            return new List<QueuedIncident>();
        }
    }

    public void Save(IReadOnlyList<QueuedIncident> items)
    {
        try
        {
            File.WriteAllText(PathOf, System.Text.Json.JsonSerializer.Serialize(items));
        }
        catch (Exception)
        {
            // Une file perdue n'est pas grave : l'envoi d'un incident ne doit jamais faire tomber l'application.
        }
    }
}
