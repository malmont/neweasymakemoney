using System.Text;
using Newtonsoft.Json;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Construit de faux jetons JWT non signés pour les tests sans dépendance réseau ni vrai secret.</summary>
internal static class TestJwtFactory
{
    public static string Create(
        string tenantCode = "DEM042",
        string email = "admin@example.test",
        TimeSpan? expiresIn = null,
        int userId = 42,
        params string[] roles)
    {
        var effectiveRoles = roles.Length > 0 ? roles : ["ROLE_ADMIN", "ROLE_USER"];
        var exp = new DateTimeOffset(DateTime.UtcNow.Add(expiresIn ?? TimeSpan.FromHours(1))).ToUnixTimeSeconds();

        static string EncodePart(object value) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        var header = EncodePart(new { alg = "none", typ = "JWT" });
        var payload = EncodePart(new
        {
            roles = effectiveRoles,
            tenant_code = tenantCode,
            id = userId,
            email,
            firstName = "Admin",
            lastName = "Test",
            username = email,
            exp
        });

        return $"{header}.{payload}.";
    }
}

