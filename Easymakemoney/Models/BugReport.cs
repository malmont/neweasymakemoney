using System.Globalization;

namespace Easymakemoney.Models
{
    /// <summary>Signalement d'un problème par le personnel (lot INC-4) : les 9 clés de POST et GET /api/incidents/bug-reports.</summary>
    public class BugReport
    {
        public int id { get; set; }
        public string? app { get; set; }
        public string? appVersion { get; set; }
        public string? screen { get; set; }
        public string description { get; set; } = string.Empty;
        public string status { get; set; } = "received";
        public string? answer { get; set; }
        public string? createdAt { get; set; }
        public string? answeredAt { get; set; }

        public string StatusText => BugReportRules.StatusText(status);
        public bool HasAnswer => !string.IsNullOrWhiteSpace(answer);
        public string AnswerText => $"Réponse de l'équipe : {answer}";

        public string HeaderText
        {
            get
            {
                var parts = new List<string> { $"Signalement n° {id}" };
                if (BugReportRules.Day(createdAt) is { Length: > 0 } day)
                    parts.Add(day);
                if (!string.IsNullOrWhiteSpace(screen))
                    parts.Add(screen!);
                return string.Join(" · ", parts);
            }
        }
    }

    public class BugReportList
    {
        public List<BugReport> reports { get; set; } = new();
    }

    public class BugReportResult
    {
        public BugReport? report { get; set; }
    }

    /// <summary>État de la transmission d'une demande d'assistance à l'équipe technique de la plateforme.</summary>
    public class SupportPlatform
    {
        public string status { get; set; } = "transmitted";
        public string? answer { get; set; }

        public string StatusText => status switch
        {
            "fixed" => "Équipe technique : corrigé",
            "wont_fix" => "Équipe technique : ne sera pas corrigé",
            _ => "Transmis à l'équipe technique",
        };

        public bool HasAnswer => !string.IsNullOrWhiteSpace(answer);
        public string AnswerText => $"Réponse de l'équipe : {answer}";
    }

    public class SupportEscaladeResult
    {
        public SupportPlatform? platform { get; set; }
    }

    public static class BugReportRules
    {
        public const string App = "gestion";
        public const int MinDescription = 10;
        public const int MaxDescription = 2000;
        public const int MinSummary = 20;
        public const int MaxSummary = 2000;
        public const long MaxScreenshotBytes = 5 * 1024 * 1024;

        /// <summary>Seuls ces comptes du personnel peuvent signaler ; le serveur refuse tous les autres (403).</summary>
        public static readonly string[] AllowedRoles = { "ROLE_ADMIN", "ROLE_POS_MANAGER", "ROLE_USER_POS" };

        public static bool CanReport(IEnumerable<string>? roles) =>
            roles != null && roles.Any(role => AllowedRoles.Contains(role, StringComparer.Ordinal));

        /// <summary>Où se trouvait le problème : noms stables d'écran, jamais d'identifiant.</summary>
        public static readonly (string Code, string Label)[] Sections =
        {
            ("Accueil", "Accueil"),
            ("Commandes", "Commandes et achats"),
            ("Articles", "Articles et stock"),
            ("Periodes", "Périodes et budget"),
            ("Statistiques", "Statistiques"),
            ("Tresorerie", "Trésorerie"),
            ("Pertes", "Prévention des pertes"),
            ("Assistance", "Assistance"),
            ("Autre", "Autre"),
        };

        public static string StatusText(string? status) => status switch
        {
            "in_progress" => "En cours",
            "fixed" => "Corrigé",
            "wont_fix" => "Ne sera pas corrigé",
            _ => "Reçu",
        };

        public static string? ContentTypeOf(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => null,
        };

        public static string Day(string? iso)
        {
            if (string.IsNullOrWhiteSpace(iso) || !DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at))
                return string.Empty;
            return Utilities.FrenchDate.Format(at.ToLocalTime().DateTime, "d MMMM yyyy");
        }
    }
}
