using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Easymakemoney.Services.Incidents;

/// <summary>
/// Rapport d'incident envoyé à POST /api/incidents/report (lot INC-2). Jamais de donnée personnelle :
/// ni nom, ni courriel, ni texte saisi, ni corps de requête, ni nom de l'appareil, ni identifiant de l'utilisateur.
/// </summary>
public class IncidentReport
{
    public const string App = "gestion";

    [JsonPropertyName("app")] public string app { get; set; } = App;
    [JsonPropertyName("appVersion")] public string? appVersion { get; set; }
    [JsonPropertyName("screen")] public string? screen { get; set; }
    [JsonPropertyName("route")] public string? route { get; set; }
    [JsonPropertyName("level")] public string level { get; set; } = IncidentLevel.Error;
    [JsonPropertyName("message")] public string message { get; set; } = string.Empty;
    [JsonPropertyName("context")] public Dictionary<string, object> context { get; set; } = new();
}

public static class IncidentLevel
{
    public const string Crash = "crash";
    public const string Error = "error";
    public const string Warning = "warning";
}

/// <summary>Nettoyage des champs avant l'envoi : le masque du serveur est un filet, pas une permission.</summary>
public static partial class IncidentSanitizer
{
    public const int MaxBytes = 4096;
    public const int MaxMessage = 200;
    public const int MaxScreen = 160;
    public const int MaxStack = 1500;
    public const int MaxStackLines = 10;

    [GeneratedRegex(@"\d{5,}")]
    private static partial Regex LongDigits();

    [GeneratedRegex(@"\d")]
    private static partial Regex AnyDigit();

    [GeneratedRegex(@"^(\d+|[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$")]
    private static partial Regex IdSegment();

    /// <summary>« Type: message » ; réduit au type si le message peut recopier une valeur (adresse, guillemets, longue suite de chiffres).</summary>
    public static string Message(Exception ex)
    {
        var type = ex.GetType().FullName ?? ex.GetType().Name;
        var text = ex.Message ?? string.Empty;
        if (text.Contains('@') || text.Contains('"') || text.Contains('«') || text.Contains('\'') || LongDigits().IsMatch(text))
            return Cut(type, MaxMessage);
        return Cut(string.IsNullOrWhiteSpace(text) ? type : $"{type}: {text}", MaxMessage);
    }

    /// <summary>Les 10 premières lignes « at … », sans chemin de dossier (le nom du fichier et la ligne seulement).</summary>
    public static string? Stack(Exception ex)
    {
        if (string.IsNullOrWhiteSpace(ex.StackTrace))
            return null;
        var lines = ex.StackTrace
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("at ", StringComparison.Ordinal))
            .Take(MaxStackLines)
            .Select(StripFolders);
        var stack = string.Join("\n", lines);
        return stack.Length == 0 ? null : Cut(stack, MaxStack);
    }

    private static string StripFolders(string line)
    {
        var marker = line.IndexOf(" in ", StringComparison.Ordinal);
        if (marker < 0)
            return line;
        var location = line[(marker + 4)..];
        var file = location.Split('/', '\\').Last();
        return $"{line[..marker]} in {file}";
    }

    /// <summary>Adresse Shell → nom stable : sans requête ni ancre, identifiants remplacés par {id}.</summary>
    public static string? Screen(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return null;
        var path = location.Split('?', '#')[0];
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => IdSegment().IsMatch(segment) ? "{id}" : segment);
        var screen = string.Join("/", segments);
        return screen.Length == 0 ? null : Cut(screen, MaxScreen);
    }

    /// <summary>Chemin du backend appelé, identifiants remplacés : /api/orders/{id}.</summary>
    public static string? Route(Uri? uri)
    {
        if (uri == null)
            return null;
        var path = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString.Split('?', '#')[0];
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => IdSegment().IsMatch(segment) ? "{id}" : segment);
        return Cut("/" + string.Join("/", segments), MaxScreen);
    }

    /// <summary>Empreinte locale : app + écran + message, chiffres remplacés par #.</summary>
    public static string Fingerprint(IncidentReport report) =>
        AnyDigit().Replace($"{report.app}|{report.screen}|{report.message}", "#");

    /// <summary>Corps JSON de 4 Ko au plus : la pile part d'abord, puis le message est raccourci.</summary>
    public static string Serialize(IncidentReport report)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(report);
        if (Encoding.UTF8.GetByteCount(json) <= MaxBytes)
            return json;
        report.context.Remove("stack");
        json = System.Text.Json.JsonSerializer.Serialize(report);
        if (Encoding.UTF8.GetByteCount(json) <= MaxBytes)
            return json;
        report.message = Cut(report.message, 80);
        report.screen = report.screen == null ? null : Cut(report.screen, 60);
        report.route = report.route == null ? null : Cut(report.route, 60);
        return System.Text.Json.JsonSerializer.Serialize(report);
    }

    private static string Cut(string text, int max) => text.Length <= max ? text : text[..max];
}
