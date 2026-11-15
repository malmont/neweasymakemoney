using System.Globalization;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Rapport PDF de la gestion (backend DOC-1 et DOC-2, /api/gestion/rapports) : réponse de la console, bilan de période ou d'année.
    /// Le serveur le fabrique en tâche de fond (pending → done | failed) et le garde 90 jours ; le fichier n'a jamais de lien public.
    /// </summary>
    public class GestionReport
    {
        public int id { get; set; }
        public string? kind { get; set; }
        public string? state { get; set; }
        public string? title { get; set; }
        public string? locale { get; set; }
        public DateTimeOffset? createdAt { get; set; }
        public DateTimeOffset? readyAt { get; set; }
        public DateTimeOffset? expiresAt { get; set; }
        public int? pages { get; set; }
        public long? size { get; set; }
        public GestionReportDownload? download { get; set; }
        public GestionReportAdvice? advice { get; set; }
        public GestionReportError? error { get; set; }
        public bool reused { get; set; }
        public int unitsCharged { get; set; }

        public bool IsDone => state == "done";
        public bool IsPending => state == "pending";
        public bool IsFailed => state == "failed";

        public string KindText => kind switch
        {
            "period" => "Bilan de période",
            "year" => "Bilan de l'année",
            "console.answer" => "Réponse de la console",
            _ => "Rapport",
        };

        public string StateText => state switch
        {
            "pending" => "En préparation…",
            "done" => expiresAt.HasValue ? $"Prêt, disponible jusqu'au {expiresAt.Value.ToLocalTime().ToString("d MMM yyyy", CultureInfo.GetCultureInfo("fr-FR"))}" : "Prêt",
            "failed" => error?.message ?? "Le rapport n'a pas pu être fabriqué.",
            _ => string.Empty,
        };

        public string DetailText
        {
            get
            {
                var parts = new List<string> { KindText };
                if (pages is > 0)
                    parts.Add(pages == 1 ? "1 page" : $"{pages} pages");
                if (size is > 0)
                    parts.Add($"{Math.Max(1, (int)Math.Round(size.Value / 1024.0))} Ko");
                if (advice?.included == true)
                    parts.Add("avec l'avis de l'IA");
                else if (advice is { included: false })
                    parts.Add("avis de l'IA indisponible");
                return string.Join(" · ", parts);
            }
        }
    }

    public class GestionReportDownload
    {
        public string? url { get; set; }
        public string? filename { get; set; }
        public string? contentType { get; set; }
    }

    public class GestionReportAdvice
    {
        public bool included { get; set; }
        public string? code { get; set; }
    }

    public class GestionReportError
    {
        public string? code { get; set; }
        public string? message { get; set; }
    }

    public class GestionReportList
    {
        public List<GestionReport> reports { get; set; } = new();
        public int page { get; set; }
        public int pages { get; set; }
        public int total { get; set; }
    }
}
