using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Easymakemoney.Services.Session;

namespace Easymakemoney.Services.Offline;

/// <summary>Dernières réponses gardées sur l'appareil, pour lecture hors connexion.</summary>
public interface IResponseCache
{
    void Save(string key, string body, DateTime savedAt);
    (string Body, DateTime SavedAt)? Load(string key);
    void Clear();
}

/// <summary>Fichiers dans le dossier privé de l'appli ; vidé au changement de site (SiteDataCleaner).</summary>
public class FileResponseCache : IResponseCache
{
    private const long MaxBytes = 2 * 1024 * 1024;

    private static string Folder => Path.Combine(FileSystem.Current.AppDataDirectory, "offline-cache");

    private static string PathOf(string key) =>
        Path.Combine(Folder, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))) + ".json");

    public void Save(string key, string body, DateTime savedAt)
    {
        try
        {
            if (Encoding.UTF8.GetByteCount(body) > MaxBytes)
                return;
            Directory.CreateDirectory(Folder);
            File.WriteAllText(PathOf(key), savedAt.ToString("O", CultureInfo.InvariantCulture) + "\n" + body);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FileResponseCache] Copie non enregistrée : {ex.Message}");
        }
    }

    public (string Body, DateTime SavedAt)? Load(string key)
    {
        try
        {
            var path = PathOf(key);
            if (!File.Exists(path))
                return null;
            var text = File.ReadAllText(path);
            var newline = text.IndexOf('\n');
            if (newline <= 0 || !DateTime.TryParse(text[..newline], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var savedAt))
                return null;
            return (text[(newline + 1)..], savedAt);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FileResponseCache] Copie illisible : {ex.Message}");
            return null;
        }
    }

    public void Clear()
    {
        try
        {
            if (Directory.Exists(Folder))
                Directory.Delete(Folder, recursive: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FileResponseCache] Effacement impossible : {ex.Message}");
        }
    }
}

/// <summary>
/// Hors connexion : l'écran montre la dernière réponse connue, datée. L'en-tête de l'appli (AppHeader) l'annonce.
/// </summary>
public static class OfflineState
{
    public static DateTime? ServedAt { get; private set; }

    public static event Action? Changed;

    public static void Served(DateTime savedAt)
    {
        ServedAt = savedAt;
        Changed?.Invoke();
    }

    public static void Online()
    {
        if (ServedAt == null)
            return;
        ServedAt = null;
        Changed?.Invoke();
    }

    public static string Text(DateTime? servedAt, DateTime? now = null)
    {
        if (servedAt is not { } at)
            return string.Empty;
        var culture = CultureInfo.GetCultureInfo("fr-CA");
        return at.Date == (now ?? DateTime.Now).Date
            ? $"Hors connexion : données de {at.ToString("H' h 'mm", culture)}"
            : $"Hors connexion : données du {at.ToString("d MMMM 'à' H' h 'mm", culture)}";
    }
}

/// <summary>
/// Garde la dernière réponse des lectures utiles en déplacement (statistiques, trésorerie) et la sert quand le réseau manque.
/// Seulement des GET réussis ; jamais une écriture. Copie propre au site et au compte. L'Accueil et « Ma journée » ont leur propre
/// copie (HomeSnapshotStore) et leur propre bandeau.
/// </summary>
public class OfflineCacheHandler : DelegatingHandler
{
    private static readonly string[] CachedPrefixes =
    {
        "gestion/statistiques", "gestion/tresorerie", "gestion/locations/statistiques",
    };

    private readonly IResponseCache _cache;
    private readonly ISessionService? _session;

    public OfflineCacheHandler(IResponseCache cache, ISessionService? session = null)
    {
        _cache = cache;
        _session = session;
    }

    public static bool IsCacheable(HttpRequestMessage request)
    {
        if (request.Method != HttpMethod.Get || request.RequestUri == null)
            return false;
        var path = request.RequestUri.IsAbsoluteUri ? request.RequestUri.PathAndQuery : request.RequestUri.OriginalString;
        var relative = path.TrimStart('/');
        if (relative.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
            relative = relative[4..];
        return CachedPrefixes.Any(prefix => relative.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Clé : site (en-tête du site), compte et adresse complète.</summary>
    public string? KeyOf(HttpRequestMessage request)
    {
        var site = request.Headers.TryGetValues("X-Tenant-Host", out var hosts) ? hosts.FirstOrDefault() : null;
        var user = _session?.CurrentUser?.Id;
        if (string.IsNullOrWhiteSpace(site) || user is not > 0 || request.RequestUri == null)
            return null;
        return $"{site.Trim().ToLowerInvariant()}|{user}|{request.RequestUri}";
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cacheable = IsCacheable(request);
        var key = cacheable ? KeyOf(request) : null;

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (key != null && !(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            if (_cache.Load(key) is not { } copy)
                throw;
            OfflineState.Served(copy.SavedAt);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(copy.Body, Encoding.UTF8, "application/json"),
            };
        }

        OfflineState.Online();
        if (key != null && response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent)
        {
            await response.Content.LoadIntoBufferAsync();
            _cache.Save(key, await response.Content.ReadAsStringAsync(cancellationToken), DateTime.Now);
        }
        return response;
    }
}
