using System.Globalization;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Module « Prévention des pertes » (backend L1-a et L1-b, docs/prevention-pertes.md) : synthèse, constats, preuves et décision.
    /// Le serveur juge seul (règles, montants, textes) ; l'application affiche. Un constat n'accuse personne et ne prouve aucune perte.
    /// </summary>
    public class LossSummary
    {
        public LossPeriod? period { get; set; }
        public LossAnalysis? lastAnalysis { get; set; }
        public LossCounts counts { get; set; } = new();
        public long? amountAtStake { get; set; }
        public string? currency { get; set; }
        public LossCashDesk? cashDesk { get; set; }
        public LossStaffNotice? staffNotice { get; set; }
        public List<StatsCaveat> caveats { get; set; } = new();

        /// <summary>Pourquoi rien n'est analysé : notice aux salariés à confirmer, module non accordé…</summary>
        public string BlockedText => lastAnalysis?.blocked switch
        {
            null or "" => string.Empty,
            "STAFF_NOTICE_REQUIRED" => "L'analyse attend votre confirmation d'avoir informé vos salariés. Elle se donne sur le web, dans les réglages de la Prévention des pertes.",
            "MODULE_NOT_GRANTED" => "Le module n'est pas accordé à ce site : rien n'est analysé.",
            _ => "L'analyse est arrêtée pour l'instant.",
        };

        public bool IsBlocked => BlockedText.Length > 0;

        public string AmountText => LossFormat.Money(amountAtStake, currency);

        public string LastAnalysisText => lastAnalysis?.at is { } at && LossFormat.Date(at) is { Length: > 0 } text
            ? $"Dernière analyse : {text}"
            : "Aucune analyse n'a encore tourné.";

        public string CashDeskText
        {
            get
            {
                if (cashDesk is not { closings: > 0 } desk)
                    return string.Empty;
                var closings = $"{desk.closings} clôture{(desk.closings > 1 ? "s" : string.Empty)} de caisse";
                if (desk.discrepancyCount == 0)
                    return $"{closings}, sans écart.";
                return $"{closings}, écart total {LossFormat.Signed(desk.discrepancy, currency)} sur {desk.discrepancyCount} clôture{(desk.discrepancyCount > 1 ? "s" : string.Empty)}.";
            }
        }

        public string CaveatsText => string.Join("\n", caveats
            .Where(c => !string.IsNullOrWhiteSpace(c.text) && c.code != lastAnalysis?.blocked)
            .Select(c => $"• {c.text}"));
    }

    public class LossPeriod
    {
        public string? from { get; set; }
        public string? to { get; set; }
    }

    public class LossAnalysis
    {
        public string? at { get; set; }
        public bool pending { get; set; }
        public string? blocked { get; set; }
        public bool failed { get; set; }
    }

    public class LossCounts
    {
        public int open { get; set; }
        public int watch { get; set; }
        public int @new { get; set; }
        public int active { get; set; }
    }

    public class LossCashDesk
    {
        public int closings { get; set; }
        public long discrepancy { get; set; }
        public int discrepancyCount { get; set; }
    }

    public class LossStaffNotice
    {
        public bool required { get; set; }
        public string? confirmedAt { get; set; }
    }

    public class LossRule
    {
        public string? code { get; set; }
        public string? label { get; set; }
    }

    public class LossRegister
    {
        public int id { get; set; }
        public string? name { get; set; }
    }

    /// <summary>Une ligne de la liste des constats (et le tronc commun de la fiche).</summary>
    public class LossFinding
    {
        public int id { get; set; }
        public int number { get; set; }
        public LossRule? rule { get; set; }
        public bool personPattern { get; set; }
        public string? status { get; set; }
        public string? statusLabel { get; set; }
        public string? summary { get; set; }
        public string? storeDay { get; set; }
        public LossRegister? cashRegister { get; set; }
        public long? amountAtStake { get; set; }
        public string? currency { get; set; }
        public bool isNew { get; set; }
        public bool newEvidenceSinceDecision { get; set; }
        public bool backfill { get; set; }
        public bool exercise { get; set; }

        /// <summary>« Constat 12 » ; un exercice a un numéro négatif : « Exercice 3 ».</summary>
        public string NumberText => exercise || number < 0 ? $"Exercice {Math.Abs(number)}" : $"Constat {number}";

        public string HeaderText
        {
            get
            {
                var parts = new List<string> { NumberText };
                if (rule?.label is { Length: > 0 } label)
                    parts.Add(label);
                return string.Join(" · ", parts);
            }
        }

        public string DetailText
        {
            get
            {
                var parts = new List<string>();
                if (LossFormat.Day(storeDay) is { Length: > 0 } day)
                    parts.Add(day);
                if (cashRegister?.name is { Length: > 0 } register)
                    parts.Add(register);
                if (amountAtStake is { } amount)
                    parts.Add($"{LossFormat.Money(amount, currency)} en jeu");
                return string.Join(" · ", parts);
            }
        }

        public string BadgeText => isNew ? "Nouveau" : newEvidenceSinceDecision ? "Nouvelles preuves" : backfill ? "Reprise" : string.Empty;
        public bool HasBadge => BadgeText.Length > 0;
    }

    /// <summary>La fiche d'un constat : preuves, personnes en poste, décision et historique.</summary>
    public class LossFindingDetail : LossFinding
    {
        public List<LossPerson> onDuty { get; set; } = new();
        public string? onDutyNotice { get; set; }
        public string? amountNotice { get; set; }
        public List<LossEvidence> evidence { get; set; } = new();
        public bool evidenceIntact { get; set; } = true;
        public LossDecision? decision { get; set; }
        public List<LossHistoryEntry> history { get; set; } = new();

        /// <summary>Une personne n'est citée qu'une fois, avec tous ses rôles (« Vente, Fermeture de la caisse »).</summary>
        public List<LossPersonLine> OnDutyLines => onDuty
            .GroupBy(person => person.id)
            .Select(group => new LossPersonLine(group.First().name ?? "—",
                string.Join(", ", group.Select(person => person.roleLabel).Where(label => !string.IsNullOrWhiteSpace(label)).Distinct())))
            .ToList();

        public bool HasOnDuty => onDuty.Count > 0;

        public string DecisionText => decision is { } d
            ? $"{statusLabel} — {d.by?.name ?? "—"}, {LossFormat.Date(d.at)}"
            : "Aucune décision pour l'instant.";

        public string? DecisionNote => decision?.note;
        public bool HasDecisionNote => !string.IsNullOrWhiteSpace(decision?.note);
    }

    public class LossPerson
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? role { get; set; }
        public string? roleLabel { get; set; }
    }

    public sealed record LossPersonLine(string Name, string Roles)
    {
        public bool HasRoles => Roles.Length > 0;
    }

    public class LossEvidence
    {
        public int id { get; set; }
        public string? kind { get; set; }
        public string? at { get; set; }
        public string? atLabel { get; set; }
        public string? title { get; set; }
        public List<LossEvidenceLine> lines { get; set; } = new();

        public string WhenText => atLabel ?? LossFormat.Date(at);
    }

    public class LossEvidenceLine
    {
        public string? label { get; set; }
        public string? value { get; set; }
    }

    public class LossDecision
    {
        public string? status { get; set; }
        public string? note { get; set; }
        public string? at { get; set; }
        public LossPerson? by { get; set; }
    }

    public class LossHistoryEntry
    {
        public string? at { get; set; }
        public string? status { get; set; }
        public string? statusLabel { get; set; }
        public bool hasNote { get; set; }
        public LossPerson? by { get; set; }

        public string Text => $"{LossFormat.Date(at)} · {statusLabel ?? status} · {by?.name ?? "—"}{(hasNote ? " · avec une note" : string.Empty)}";
    }

    /// <summary>Mode exercice (EX-1) : scénarios, exercices du site et plafonds ; ouvert même sans le droit du module.</summary>
    public class LossExercises
    {
        public List<LossScenario> scenarios { get; set; } = new();
        public List<LossFinding> findings { get; set; } = new();
        public int count { get; set; }
        public int limit { get; set; }
        public int remaining { get; set; }
        public LossDaily? daily { get; set; }
        public string? notice { get; set; }

        /// <summary>« 29 exercices possibles · 12 lancements aujourd'hui » ; le plafond du jour n'est cité que s'il est bas.</summary>
        public string LimitsText
        {
            get
            {
                var parts = new List<string> { $"{remaining} exercice{(remaining > 1 ? "s" : string.Empty)} encore possible{(remaining > 1 ? "s" : string.Empty)} sur {limit}" };
                if (daily is { } d && d.remaining <= 30)
                    parts.Add(d.remaining == 0 ? "lancements du jour épuisés" : $"{d.remaining} lancement{(d.remaining > 1 ? "s" : string.Empty)} aujourd'hui");
                return string.Join(" · ", parts);
            }
        }

        public bool CanLaunch => remaining > 0 && (daily?.remaining ?? 1) > 0;
    }

    public class LossDaily
    {
        public int limit { get; set; }
        public int remaining { get; set; }
        public string? resetAt { get; set; }
    }

    public class LossScenario
    {
        public string? code { get; set; }
        public string? label { get; set; }
        public string? description { get; set; }
        public string? learn { get; set; }
        public LossRule? rule { get; set; }
        public bool personPattern { get; set; }
        public bool available { get; set; } = true;
        public bool IsUnavailable => !available;

        public string LearnText => string.IsNullOrWhiteSpace(learn) ? string.Empty : $"Ce que vous y apprenez : {learn}";
        public bool HasLearn => LearnText.Length > 0;
    }

    public class LossExerciseLaunch
    {
        public LossFindingDetail? finding { get; set; }
        public LossAlertPreview? alertPreview { get; set; }
        public LossTestEmail? testEmail { get; set; }
        public int remaining { get; set; }

        /// <summary>Ce que l'alerte aurait dit, et le sort du courriel d'essai.</summary>
        public string PreviewText
        {
            get
            {
                var lines = new List<string>();
                if (alertPreview?.tile is { } tile)
                    lines.Add($"Vignette de l'accueil : « {tile.text} »");
                if (alertPreview?.email is { } email)
                    lines.Add($"Courriel du jour : « {email.subject} »");
                if (testEmail is { requested: true } test)
                    lines.Add(test.sent
                        ? "Le courriel d'essai vous a été envoyé."
                        : test.reason == "DAILY_LIMIT_REACHED"
                            ? "L'exercice est lancé, mais le courriel d'essai n'est pas parti : limite du jour atteinte."
                            : "Le courriel d'essai n'est pas parti.");
                return string.Join("\n", lines);
            }
        }
    }

    public class LossAlertPreview
    {
        public LossAlertText? tile { get; set; }
        public LossAlertMail? email { get; set; }
    }

    public class LossAlertText
    {
        public string? title { get; set; }
        public string? text { get; set; }
    }

    public class LossAlertMail
    {
        public string? subject { get; set; }
        public string? message { get; set; }
    }

    public class LossTestEmail
    {
        public bool requested { get; set; }
        public bool sent { get; set; }
        public string? reason { get; set; }
    }

    public static class LossFormat
    {
        public static string Money(long? cents, string? currency) =>
            cents is { } value ? SiteCurrency.FormatCents(value, null, currency) : "—";

        /// <summary>« −12,00 $ » : un écart de caisse garde son signe.</summary>
        public static string Signed(long cents, string? currency)
        {
            var text = SiteCurrency.FormatCents(Math.Abs(cents), null, currency);
            return cents > 0 ? $"+{text}" : cents < 0 ? $"−{text}" : text;
        }

        public static string Day(string? isoDay) =>
            DateTime.TryParseExact(isoDay, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
                ? day.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("fr-CA"))
                : string.Empty;

        public static string Date(string? iso) =>
            DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at)
                ? at.LocalDateTime.ToString("d MMM yyyy, HH 'h' mm", CultureInfo.GetCultureInfo("fr-CA"))
                : string.Empty;
    }
}
