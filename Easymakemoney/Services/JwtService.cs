using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Easymakemoney.Models.Session;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Services;

public class JwtService : IJwtService
{
    private readonly JwtSecurityTokenHandler _handler = new();

    public bool TryParseAndValidate(string token, string expectedTenantCode, out UserInfo? user, out string? errorMessage)
    {
        user = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(token))
        {
            errorMessage = "Jeton d'authentification manquant.";
            return false;
        }

        JwtSecurityToken jsonToken;
        try
        {
            jsonToken = _handler.ReadJwtToken(token);
        }
        catch (Exception)
        {
            errorMessage = "Jeton d'authentification invalide.";
            return false;
        }

        // 1. Extraction et contrôle de tous les rôles
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in jsonToken.Claims)
        {
            if (claim.Type == "roles" || claim.Type == ClaimTypes.Role)
            {
                roles.Add(claim.Value);
            }
        }

        // Prise en charge des rôles encodés en tableau JSON dans le payload
        if (jsonToken.Payload.TryGetValue("roles", out var rawRoles))
        {
            if (rawRoles is JArray jArray)
            {
                foreach (var item in jArray)
                {
                    var val = item?.ToString();
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        roles.Add(val);
                    }
                }
            }
            else if (rawRoles is IEnumerable<object> objList)
            {
                foreach (var item in objList)
                {
                    var val = item?.ToString();
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        roles.Add(val);
                    }
                }
            }
            else if (rawRoles is string singleRole && !string.IsNullOrWhiteSpace(singleRole))
            {
                roles.Add(singleRole);
            }
        }

        // Administrateur du site, ou compte Assistance (limité au module Assistance, voir UserInfo.IsSupportOnly)
        if (!roles.Contains(UserInfo.RoleAdmin) && !roles.Contains(UserInfo.RoleSupport))
        {
            errorMessage = "Ce compte n'a pas accès à l'application de gestion.";
            return false;
        }

        // 2. Extraction et contrôle du tenant_code
        string? tenantCode = null;
        if (jsonToken.Payload.TryGetValue("tenant_code", out var rawTenantCode))
        {
            tenantCode = rawTenantCode?.ToString();
        }
        else
        {
            tenantCode = jsonToken.Claims.FirstOrDefault(c => c.Type == "tenant_code")?.Value;
        }

        if (string.IsNullOrWhiteSpace(tenantCode) ||
            !string.Equals(tenantCode.Trim(), expectedTenantCode?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Ce compte n'appartient pas à ce site.";
            return false;
        }

        // 3. Extraction des autres informations utilisateur
        int userId = 0;
        if (jsonToken.Payload.TryGetValue("id", out var rawId) && int.TryParse(rawId?.ToString(), out var parsedId))
        {
            userId = parsedId;
        }
        else
        {
            var idClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "id" || c.Type == ClaimTypes.NameIdentifier);
            if (idClaim != null && int.TryParse(idClaim.Value, out var parsedClaimId))
            {
                userId = parsedClaimId;
            }
        }

        var email = jsonToken.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == ClaimTypes.Email)?.Value
                    ?? jsonToken.Payload.GetValueOrDefault("email")?.ToString()
                    ?? string.Empty;

        var firstName = jsonToken.Claims.FirstOrDefault(c => c.Type == "firstName")?.Value
                        ?? jsonToken.Payload.GetValueOrDefault("firstName")?.ToString()
                        ?? string.Empty;

        var lastName = jsonToken.Claims.FirstOrDefault(c => c.Type == "lastName")?.Value
                       ?? jsonToken.Payload.GetValueOrDefault("lastName")?.ToString()
                       ?? string.Empty;

        var username = jsonToken.Claims.FirstOrDefault(c => c.Type == "username")?.Value
                       ?? jsonToken.Payload.GetValueOrDefault("username")?.ToString()
                       ?? email;

        user = new UserInfo
        {
            Id = userId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Roles = roles.ToList(),
            TenantCode = tenantCode,
            Username = username
        };

        return true;
    }

    public DateTime? GetExpirationUtc(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var jsonToken = _handler.ReadJwtToken(token);
            return jsonToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }
}
