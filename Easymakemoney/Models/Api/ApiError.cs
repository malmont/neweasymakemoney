using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Models.Api;

public class ApiError
{
    private static readonly Regex ValidCodeRegex = new("^[A-Z][A-Z0-9_]*$", RegexOptions.Compiled);

    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? RetryAfterSeconds { get; set; }

    public static ApiError FromHttpResponse(string? json, HttpStatusCode statusCode, HttpResponseHeaders? headers)
    {
        string? extractedCode = null;
        string? serverMessage = null;
        int? retryAfterSeconds = null;

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
            RetryAfterSeconds = retryAfterSeconds
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
            _ => (int)statusCode switch
            {
                429 => $"Trop de tentatives. Veuillez patienter {retryAfterSeconds ?? 60} secondes.",
                >= 500 and <= 599 => "Erreur du serveur. Veuillez réessayer plus tard.",
                _ => "Une erreur est survenue lors de la communication avec le serveur."
            }
        };
    }
}
