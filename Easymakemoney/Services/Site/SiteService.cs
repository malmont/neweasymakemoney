using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Easymakemoney.Models.Api;
using Easymakemoney.Models.Tenant;
using Easymakemoney.Services.Tenant;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Services.Site;

public class SiteService : ISiteService
{
    private static readonly Regex AppKeyFormatRegex = new("^IZ[A-Z]{3}[A-Z0-9]{12}$", RegexOptions.Compiled);
    private static readonly Regex NonAlphaNumericRegex = new("[^a-zA-Z0-9]", RegexOptions.Compiled);

    private readonly HttpClient _httpClient;

    public SiteService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Fonction pure de contrôle de format de la clé d'activation.
    /// Retire tout ce qui n'est ni lettre ni chiffre, met en majuscules, et vérifie le motif ^IZ[A-Z]{3}[A-Z0-9]{12}$.
    /// </summary>
    public static bool ValidateAppKeyFormat(string? input, out string cleanedKey)
    {
        cleanedKey = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalized = NonAlphaNumericRegex.Replace(input, string.Empty).ToUpperInvariant();
        if (AppKeyFormatRegex.IsMatch(normalized))
        {
            cleanedKey = normalized;
        }

        return !string.IsNullOrEmpty(cleanedKey);
    }

    /// <summary>
    /// Fonction pure de nettoyage de la saisie d'un code de site / domaine / code court.
    /// Minuscules, retrait des espaces, http://, https://, www., chemin et port.
    /// </summary>
    public static string CleanSiteInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var s = input.Trim().ToLowerInvariant().Replace(" ", string.Empty);

        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            s = s["http://".Length..];
        }
        else if (s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            s = s["https://".Length..];
        }

        if (s.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            s = s["www.".Length..];
        }

        var slashIndex = s.IndexOf('/');
        if (slashIndex >= 0)
        {
            s = s[..slashIndex];
        }

        var colonIndex = s.IndexOf(':');
        if (colonIndex >= 0)
        {
            s = s[..colonIndex];
        }

        return s.Trim();
    }

    public async Task<SiteResult> ActivateWithKeyAsync(string rawKey, CancellationToken cancellationToken = default)
    {
        if (!ValidateAppKeyFormat(rawKey, out _))
        {
            return SiteResult.Failure("Le format de la clé d'activation est invalide (attendu : IZ-ADM-XXXX-XXXX-XXXX).");
        }

        var deviceName = $"{DeviceInfo.Manufacturer} {DeviceInfo.Model}".Trim();
        var requestPayload = new
        {
            key = rawKey.Trim(),
            app = "admin",
            deviceName
        };

        var requestJson = JsonConvert.SerializeObject(requestPayload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "app/activate");
        request.SetSkipTenantHost(true);
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var jObj = JObject.Parse(content);
                    var tenantToken = jObj["tenant"];
                    var host = tenantToken?["host"]?.Value<string>();
                    var code = tenantToken?["code"]?.Value<string>();
                    var name = tenantToken?["name"]?.Value<string>();
                    var label = jObj["label"]?.Value<string>();

                    // Règle L6 : vérifier que tenant.host et tenant.code sont présents
                    if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(code))
                    {
                        return SiteResult.Failure("Réponse du serveur incomplète.");
                    }

                    var tenantContext = new TenantContext
                    {
                        Host = host,
                        Code = code,
                        Name = name ?? code,
                        Label = label
                    };

                    return SiteResult.Success(tenantContext);
                }
                catch (Exception)
                {
                    return SiteResult.Failure("Réponse du serveur incomplète.");
                }
            }

            var apiError = ApiError.FromHttpResponse(content, response.StatusCode, response.Headers);
            return SiteResult.Failure(apiError.Message, apiError);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Règle L6 : pas de connexion sur HttpRequestException ou TaskCanceledException
            return SiteResult.Failure("Pas de connexion.");
        }
    }

    public async Task<SiteResult> CheckSiteCodeAsync(string rawInput, CancellationToken cancellationToken = default)
    {
        var cleanedInput = CleanSiteInput(rawInput);
        // Règle L4 : si la saisie est vide après nettoyage, s'arrêter avant l'appel
        if (string.IsNullOrWhiteSpace(cleanedInput))
        {
            return SiteResult.Failure("Saisissez le code du site.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "tenant/check-mobile");
        request.SetCustomTenantHost(cleanedInput);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var jObj = JObject.Parse(content);
                    var exists = jObj["exists"]?.Value<bool>() ?? false;
                    var tenantToken = jObj["tenant"];
                    var host = tenantToken?["host"]?.Value<string>();
                    var code = tenantToken?["code"]?.Value<string>();
                    var name = tenantToken?["name"]?.Value<string>();

                    if (!exists || string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(code))
                    {
                        return SiteResult.Failure("Site introuvable.");
                    }

                    var tenantContext = new TenantContext
                    {
                        Host = host,
                        Code = code,
                        Name = name ?? code
                    };

                    return SiteResult.Success(tenantContext);
                }
                catch (Exception)
                {
                    return SiteResult.Failure("Site introuvable.");
                }
            }

            var apiError = ApiError.FromHttpResponse(content, response.StatusCode, response.Headers);
            return SiteResult.Failure(apiError.Message, apiError);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Règle L6 : pas de connexion sur HttpRequestException ou TaskCanceledException
            return SiteResult.Failure("Pas de connexion.");
        }
    }
}
