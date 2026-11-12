namespace Easymakemoney.Models.Session;

public class UserInfo
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string TenantCode { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    public const string RoleAdmin = "ROLE_ADMIN";
    public const string RoleSupport = "ROLE_SUPPORT";

    [Newtonsoft.Json.JsonIgnore]
    public bool IsAdmin => Roles.Contains(RoleAdmin, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Compte Assistance seul (décision du 11/11/2026) : il ne voit que le module Assistance. Le serveur refuse de toute façon
    /// ses appels hors de /api/support (403 SUPPORT_SCOPE_ONLY) ; l'appli ne lui montre donc rien d'autre.
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public bool IsSupportOnly => !IsAdmin && Roles.Contains(RoleSupport, StringComparer.OrdinalIgnoreCase);
}

