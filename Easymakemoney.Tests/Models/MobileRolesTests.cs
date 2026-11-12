using System.Text;
using Easymakemoney.Services;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>
/// Décision du 12/11/2026 : l'appli de gestion mobile est réservée aux administrateurs. Le compte Assistance
/// (ROLE_SUPPORT) n'existe que pour l'appli web.
/// </summary>
public class MobileRolesTests
{
    private static string Token(params string[] roles)
    {
        static string Part(object value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{Part(new { alg = "none", typ = "JWT" })}.{Part(new { roles, tenant_code = "demo", id = 12, email = "agent@example.test" })}.";
    }

    [Fact]
    public void Admin_Accepted()
    {
        Assert.True(new JwtService().TryParseAndValidate(Token("ROLE_ADMIN", "ROLE_USER"), "demo", out var user, out _));
        Assert.Equal(12, user!.Id);
    }

    [Theory]
    [InlineData("ROLE_SUPPORT")]
    [InlineData("ROLE_USER")]
    [InlineData("ROLE_USER_INTERNET")]
    public void OtherRoles_Refused(string role)
    {
        Assert.False(new JwtService().TryParseAndValidate(Token(role, "ROLE_USER"), "demo", out var user, out var error));
        Assert.Null(user);
        Assert.Equal("Ce compte n'a pas accès à l'application de gestion.", error);
    }

    [Fact]
    public void AdminOfAnotherSite_Refused()
    {
        Assert.False(new JwtService().TryParseAndValidate(Token("ROLE_ADMIN"), "autre", out _, out var error));
        Assert.Equal("Ce compte n'appartient pas à ce site.", error);
    }
}
