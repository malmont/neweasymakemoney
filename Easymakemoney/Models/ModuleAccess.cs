using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Lot MOD : réponse 403 MODULE_INACTIVE du serveur quand le module de gestion n'est pas accordé au site.
    /// Forme : { error, code: "MODULE_INACTIVE", module: "management", feature, message, requestable, request, trial }.
    /// Lecture souple : un champ absent garde sa valeur par défaut (demande possible, pas d'essai annoncé).
    /// </summary>
    public class ModuleInactiveInfo
    {
        public const string Code = "MODULE_INACTIVE";
        public const string ManagementModule = "management";
        public const string ManagementFeature = "app.management";

        public string Module { get; set; } = string.Empty;
        public string Feature { get; set; } = ManagementFeature;
        public string Message { get; set; } = string.Empty;

        /// <summary>Le site peut demander l'essai ou l'activation depuis l'appli.</summary>
        public bool Requestable { get; set; } = true;

        /// <summary>Une demande est déjà en attente : on n'affiche pas « Demander » une deuxième fois.</summary>
        public bool Pending { get; set; }

        /// <summary>Durée de l'essai gratuit proposé (0 = pas d'essai : la demande part à la plateforme).</summary>
        public int TrialDays { get; set; }

        public bool IsManagement => Module == ManagementModule;

        public static ModuleInactiveInfo? TryParse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch
            {
                return null;
            }

            var code = Text(root["code"]) ?? Text(root["error"]);
            if (code != Code)
                return null;

            var info = new ModuleInactiveInfo
            {
                Module = Text(root["module"]) ?? string.Empty,
                Feature = Text(root["feature"]) ?? ManagementFeature,
                Message = Text(root["message"]) ?? string.Empty,
            };

            if (root["requestable"] is { Type: JTokenType.Boolean } requestable)
                info.Requestable = requestable.Value<bool>();

            // « request » : la demande en cours (objet), ou un booléen ; « pending » accepté aussi.
            var request = root["request"];
            info.Pending = request switch
            {
                { Type: JTokenType.Object } => Text(request["status"]) is null or "pending",
                { Type: JTokenType.Boolean } => request.Value<bool>(),
                _ => false,
            } || root["pending"] is { Type: JTokenType.Boolean } pending && pending.Value<bool>();

            // « trial » : { allowed, days }, ou directement le nombre de jours.
            var trial = root["trial"];
            if (trial is { Type: JTokenType.Object })
            {
                var allowed = trial["allowed"] is not { Type: JTokenType.Boolean } a || a.Value<bool>();
                info.TrialDays = allowed ? Int(trial["days"]) : 0;
            }
            else if (trial is { Type: JTokenType.Integer })
            {
                info.TrialDays = trial.Value<int>();
            }

            return info;
        }

        public string Explanation
        {
            get
            {
                if (Pending)
                    return "Votre demande est en attente. La plateforme vous répondra par courriel ; l'application s'ouvrira dès l'accord.";
                if (!Requestable)
                    return "Contactez la plateforme pour activer l'application de gestion sur ce site.";
                if (TrialDays > 0)
                    return $"Essayez-la gratuitement pendant {TrialDays} jours : l'essai s'ouvre tout de suite, sans engagement.";
                return "Demandez son activation : la plateforme vous répondra par courriel.";
            }
        }

        public bool CanRequest => Requestable && !Pending;

        public string RequestButtonText => TrialDays > 0 ? $"Essayer {TrialDays} jours gratuitement" : "Demander l'activation";

        private static string? Text(JToken? token) =>
            token is { Type: JTokenType.String } ? token.Value<string>() : null;

        private static int Int(JToken? token) =>
            token != null && int.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    /// <summary>Réponse 201 de POST /api/site/features/{clé}/request : essai ouvert, ou demande envoyée.</summary>
    public class FeatureRequestResult
    {
        public string kind { get; set; } = string.Empty;
        public JObject? entitlement { get; set; }
        public JObject? request { get; set; }

        public bool IsTrial => kind == "trial";

        /// <summary>Fin de l'essai ouvert, si le serveur la donne.</summary>
        public DateTime? TrialEndsAt
        {
            get
            {
                var text = entitlement?["endsAt"]?.ToString() ?? entitlement?["ends_at"]?.ToString();
                return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                    ? date.LocalDateTime
                    : null;
            }
        }

        public string ConfirmationText => IsTrial
            ? TrialEndsAt is { } end
                ? $"Essai ouvert jusqu'au {end.ToString("d MMMM yyyy", CultureInfo.CurrentCulture)}. L'application de gestion est disponible."
                : "Essai ouvert. L'application de gestion est disponible."
            : "Demande envoyée. La plateforme vous répondra par courriel.";
    }

    /// <summary>Lot MOD : GET /api/gestion/ia, unités IA du mois (même réserve que l'assistant du site).</summary>
    public class ManagementAi
    {
        public string tier { get; set; } = "none";
        public ManagementAiCredits? credits { get; set; }
        public ManagementAiCosts? costs { get; set; }
        public ManagementAiLimits? limits { get; set; }
        public ManagementAiToday? today { get; set; }

        public bool IsIncluded => tier is "help" or "full";

        public string CreditsText
        {
            get
            {
                if (!IsIncluded)
                    return "Aide IA non incluse dans le forfait du site : les conseils restent calculés, sans rédaction.";
                if (credits == null)
                    return string.Empty;
                var text = $"Unités IA : {credits.remaining} sur {credits.monthly} ce mois-ci";
                if (DateTimeOffset.TryParse(credits.resetAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var reset))
                    text += $", renouvelées le {reset.LocalDateTime.ToString("d MMMM", CultureInfo.CurrentCulture)}";
                return text + ".";
            }
        }

        public bool IsExhausted => IsIncluded && credits is { remaining: <= 0 };
    }

    public class ManagementAiCredits
    {
        public int monthly { get; set; }
        public int used { get; set; }
        public int remaining { get; set; }
        public string? resetAt { get; set; }
    }

    public class ManagementAiCosts
    {
        public int advice { get; set; } = 1;
        public int comment { get; set; } = 1;
        public int document { get; set; } = 1;
    }

    public class ManagementAiLimits
    {
        public int commentsPerDay { get; set; }
        public int adviceRefreshPerDay { get; set; }
    }

    public class ManagementAiToday
    {
        public int comments { get; set; }
        public int adviceRefresh { get; set; }
    }
}
