using System.Globalization;
using Easymakemoney.Utilities;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models
{
    /// <summary>
    /// Lot IA-FACT-1 : le fil d'une facture fournisseur (GET /api/gestion/conversations/{id}). Le serveur calcule tout : résumé, questions,
    /// aperçu, empreinte ; l'application affiche et renvoie les réponses. Un champ absent reste absent (aucune valeur inventée).
    /// </summary>
    public class InvoiceConversation
    {
        public int id { get; set; }
        public string? kind { get; set; }
        public string? state { get; set; }
        public string? locale { get; set; }
        public ConversationDocument? document { get; set; }
        public ConversationSummary? summary { get; set; }
        public List<ConversationMessage> messages { get; set; } = new();
        public List<ConversationQuestion> questions { get; set; } = new();
        public int? openQuestions { get; set; }
        public InvoicePreview? preview { get; set; }
        public string? previewHash { get; set; }
        public ConversationUndo? undo { get; set; }
        public ConversationCredits? credits { get; set; }

        public bool IsReading => state == "reading";
        public bool IsFailed => state == "failed";
        public bool IsAsking => state == "questions";
        public bool IsReady => state == "ready" && !string.IsNullOrEmpty(previewHash);
        public bool IsConfirmed => state == "confirmed";
        public bool IsAbandoned => state == "abandoned";

        /// <summary>Texte de l'échec de lecture, d'après le code du document.</summary>
        public string FailureText => document?.error switch
        {
            "DOCUMENT_UNREADABLE" => "La facture est illisible. Reprenez une photo plus nette.",
            "AI_TOO_MANY_FAILURES" => "La lecture a échoué plusieurs fois. Réessayez plus tard.",
            _ => "Le service de lecture est indisponible pour le moment. Aucun crédit n'a été utilisé.",
        };
    }

    public class ConversationDocument
    {
        public int id { get; set; }
        public string? status { get; set; }
        public string? error { get; set; }
        public int? commandeId { get; set; }
    }

    public class ConversationSummary
    {
        public string? text { get; set; }
        public string? supplier { get; set; }
        public string? number { get; set; }
        public string? date { get; set; }
        public int? lines { get; set; }
        public long? total { get; set; }
        public string? currency { get; set; }
        public int? openQuestions { get; set; }
    }

    public class ConversationMessage
    {
        public int id { get; set; }
        public string? role { get; set; }
        public string? kind { get; set; }
        public JToken? content { get; set; }
        public string? createdAt { get; set; }
    }

    public class ConversationQuestion
    {
        public string id { get; set; } = string.Empty;
        public string? kind { get; set; }
        public string? path { get; set; }
        public string? title { get; set; }
        public string? text { get; set; }
        public bool required { get; set; }
        public ConversationInput? input { get; set; }
        public List<ConversationOption> options { get; set; } = new();
        public JToken? answer { get; set; }
        public long? difference { get; set; }

        public bool IsAnswered => answer != null && answer.Type != JTokenType.Null;
        public string InputType => input?.type ?? "choice";

        /// <summary>L'option dont la valeur est la réponse actuelle, s'il y en a une.</summary>
        public ConversationOption? AnsweredOption => IsAnswered ? options.FirstOrDefault(o => o.value != null && JToken.DeepEquals(o.value, answer)) : null;

        /// <summary>La réponse actuelle, lisible : libellé de l'option, montant, date ou nombre ; null sans réponse.</summary>
        public string? AnswerText
        {
            get
            {
                if (!IsAnswered || answer is not { } value)
                    return null;
                if (AnsweredOption is { } option)
                    return option.label;
                if (value is JObject obj && obj["create"] is JObject create)
                    return $"Créer « {create.Value<string>("name")} »";
                return InputType switch
                {
                    "money" when value.Type is JTokenType.Integer => SiteCurrency.FormatCents(value.Value<long>(), CultureInfo.CurrentCulture, input?.currency),
                    "date" => DateTime.TryParseExact(value.Value<string>(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
                        ? FrenchDate.Format(day, "d MMMM yyyy")
                        : value.ToString(),
                    _ => value.ToString(),
                };
            }
        }
    }

    public class ConversationInput
    {
        public string? type { get; set; }
        public string? currency { get; set; }
        public double? min { get; set; }
    }

    public class ConversationOption
    {
        public JToken? value { get; set; }
        public string label { get; set; } = string.Empty;
        public double? score { get; set; }

        /// <summary>Option de création (fournisseur ou article) : son nom prérempli est modifiable avant l'envoi.</summary>
        public bool IsCreate => value is JObject obj && obj["create"] is JObject;
    }

    public class ConversationUndo
    {
        public bool available { get; set; }
        public string? until { get; set; }
        public List<ConversationBlocker> blockers { get; set; } = new();
    }

    public class ConversationBlocker
    {
        public string? code { get; set; }
        public string? message { get; set; }
    }

    public class ConversationCredits
    {
        public int? monthly { get; set; }
        public int? used { get; set; }
        public int? remaining { get; set; }
        public string? resetAt { get; set; }
    }
}
