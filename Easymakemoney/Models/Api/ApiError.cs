using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models.Api;

public class ApiValidationError
{
    public string Path { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ApiError
{
    private static readonly Regex ValidCodeRegex = new("^[A-Z][A-Z0-9_]*$", RegexOptions.Compiled);

    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? RetryAfterSeconds { get; set; }
    public List<ApiValidationError> Errors { get; set; } = new();

    public static ApiError FromHttpResponse(string? json, HttpStatusCode statusCode, HttpResponseHeaders? headers)
    {
        string? extractedCode = null;
        string? serverMessage = null;
        int? retryAfterSeconds = null;
        var validationErrors = new List<ApiValidationError>();

        // Extraction Retry-After selon L1 (Delta ou Date)
        if (headers?.RetryAfter != null)
        {
            if (headers.RetryAfter.Delta.HasValue)
            {
                retryAfterSeconds = (int)Math.Ceiling(headers.RetryAfter.Delta.Value.TotalSeconds);
            }
            else if (headers.RetryAfter.Date.HasValue)
            {
                var diff = headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(diff.TotalSeconds));
            }
        }

        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var jObject = JObject.Parse(json);

                // Règle L1 : on prend code s'il s'agit d'une chaîne respectant ^[A-Z][A-Z0-9_]*$, sinon error
                var codeToken = jObject["code"];
                if (codeToken != null && codeToken.Type == JTokenType.String)
                {
                    var codeStr = codeToken.Value<string>();
                    if (!string.IsNullOrWhiteSpace(codeStr) && ValidCodeRegex.IsMatch(codeStr))
                    {
                        extractedCode = codeStr;
                    }
                }

                if (string.IsNullOrEmpty(extractedCode))
                {
                    var errorToken = jObject["error"];
                    if (errorToken != null && errorToken.Type == JTokenType.String)
                    {
                        extractedCode = errorToken.Value<string>();
                    }
                }

                var messageToken = jObject["message"];
                if (messageToken != null && messageToken.Type == JTokenType.String)
                {
                    serverMessage = messageToken.Value<string>();
                }

                if (retryAfterSeconds == null)
                {
                    var retrySecondsToken = jObject["retryAfterSeconds"] ?? jObject["retryAfter"];
                    if (retrySecondsToken != null && int.TryParse(retrySecondsToken.ToString(), out var parsedSeconds))
                    {
                        retryAfterSeconds = parsedSeconds;
                    }
                }

                // Extraction des erreurs de validation (format Symfony "errors" ou API Platform "violations")
                var errorsToken = jObject["errors"];
                if (errorsToken is JArray jErrors)
                {
                    foreach (var item in jErrors)
                    {
                        var path = item["path"]?.ToString() ?? string.Empty;
                        var msg = item["message"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(msg))
                        {
                            validationErrors.Add(new ApiValidationError { Path = path, Message = msg });
                        }
                    }
                }

                var violationsToken = jObject["violations"];
                if (violationsToken is JArray jViolations)
                {
                    foreach (var item in jViolations)
                    {
                        var path = item["propertyPath"]?.ToString() ?? item["path"]?.ToString() ?? string.Empty;
                        var msg = item["message"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(msg))
                        {
                            validationErrors.Add(new ApiValidationError { Path = path, Message = msg });
                        }
                    }
                }

                // Extraction des erreurs de validation par champ (format backend G1 "fields")
                var fieldsToken = jObject["fields"];
                if (fieldsToken is JObject jFields)
                {
                    foreach (var prop in jFields.Properties())
                    {
                        var rawField = prop.Name;
                        var errorCode = prop.Value?.ToString() ?? string.Empty;
                        var friendlyPath = TranslateFieldName(rawField);
                        var friendlyMessage = TranslateValidationCode(errorCode);
                        validationErrors.Add(new ApiValidationError { Path = friendlyPath, Message = friendlyMessage });
                    }
                }
            }
            catch
            {
                // Corps non JSON
            }
        }

        if ((int)statusCode == 429 && (retryAfterSeconds == null || retryAfterSeconds <= 0))
        {
            retryAfterSeconds = 60;
        }

        extractedCode ??= statusCode.ToString();

        var fallbackMessage = GetFallbackMessage(extractedCode, statusCode, retryAfterSeconds);
        var finalMessage = !string.IsNullOrWhiteSpace(serverMessage) ? serverMessage : fallbackMessage;

        return new ApiError
        {
            Code = extractedCode,
            Message = finalMessage,
            RetryAfterSeconds = retryAfterSeconds,
            Errors = validationErrors
        };
    }

    public static string TranslateFieldName(string field)
    {
        return field switch
        {
            "budget" or "budgetCollection" => "Budget",
            "nomCollection" or "name" => "Nom",
            "date" => "Date",
            "montant" => "Montant",
            "price" => "Prix",
            "transporteur" or "transporteur.id" => "Transporteur",
            "fournisseur" or "fournisseur.id" => "Fournisseur",
            "typeNoteDeFraisId" => "Type",
            "coefficientMultiplier" => "Coefficient",
            _ => string.Empty
        };
    }

    public static string TranslateValidationCode(string errorCode)
    {
        return errorCode switch
        {
            "MUST_BE_INTEGER_CENTS" => "Montant invalide.",
            "REQUIRED" => "Ce champ est obligatoire.",
            "MUST_BE_NUMBER" => "Ce champ doit être un nombre valide.",
            "INVALID_DATE" => "La date est invalide.",
            "INVALID_ID" => "L'identifiant est invalide.",
            "OUTSIDE_PERIOD" => "La date doit être comprise dans cette période.",
            "BEFORE_START" => "La date de fin doit suivre la date de début.",
            "MUST_BE_POSITIVE" => "Le montant doit être supérieur à zéro.",
            "EXCHANGE_RATE_REQUIRED" => "Saisissez le taux de change.",
            "INVALID_CURRENCY" => "Devise inconnue.",
            "DATE_IN_FUTURE" => "La date ne peut pas être dans le futur.",
            _ => !string.IsNullOrWhiteSpace(errorCode) ? errorCode : "Valeur invalide."
        };
    }

    public static string GetFallbackMessage(string code, HttpStatusCode statusCode, int? retryAfterSeconds)
    {
        return code switch
        {
            "APP_KEY_MISSING" => "Veuillez saisir une clé d'activation.",
            "APP_KEY_INVALID" => "Cette clé d'activation est invalide, inconnue ou expirée.",
            "APP_KEY_WRONG_APP" => "Cette clé d'activation est destinée à une autre application.",
            "TENANT_NOT_FOUND" => "Site introuvable.",
            "TENANT_HOST_MISSING" => "Saisissez le code du site.",
            "RATE_LIMITED" or "TOO_MANY_ATTEMPTS" => $"Trop de tentatives. Veuillez patienter {retryAfterSeconds ?? 60} secondes.",
            "VALIDATION_FAILED" => "Données non valides.",
            "ALREADY_CLOSED" => "Cet élément est déjà clôturé.",
            "PERIOD_CLOSED" => "Cette période est clôturée : plus rien ne peut y être ajouté.",
            "PERIOD_NOT_DELETABLE" => "Une période ne peut pas être supprimée.",
            "NOT_CLOSED" => "Cette période est déjà ouverte.",
            "TRIP_OVERLAP" => "Ces dates chevauchent un autre voyage.",
            "TRIP_NOT_FOUND" => "Ce voyage n'existe plus.",
            "BUDGET_SOURCE_NOT_FOUND" => "Aucun budget à reprendre pour la même période l'an dernier.",
            "BALANCE_DATE_TAKEN" => "Un solde est déjà saisi à cette date.",
            "LINE_RECEIVED" => "Cette ligne a déjà été reçue en stock : annulez d'abord la réception.",
            "PRODUCT_IN_USE" => "Cet article a des ventes, des achats ou des mouvements de stock : retirez-le de la vente au lieu de le supprimer.",
            _ => (int)statusCode switch
            {
                429 => $"Trop de tentatives. Veuillez patienter {retryAfterSeconds ?? 60} secondes.",
                >= 500 and <= 599 => "Erreur du serveur. Veuillez réessayer plus tard.",
                _ => "Une erreur est survenue lors de la communication avec le serveur."
            }
        };
    }
}
