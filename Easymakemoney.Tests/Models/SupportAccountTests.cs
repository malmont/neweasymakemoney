using System.Text;
using Easymakemoney.Models.Session;
using Easymakemoney.Services;
using Easymakemoney.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>ASS-M2 : un compte Assistance seul se connecte et n'arrive que sur la boîte des demandes.</summary>
public class SupportAccountTests
{
    private static string Token(params string[] roles)
    {
        static string Part(object value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var header = Part(new { alg = "none", typ = "JWT" });
        var payload = Part(new { roles, tenant_code = "demo", id = 12, email = "agent@example.test", exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() });
        return $"{header}.{payload}.";
    }

    [Fact]
    public void Jwt_Admin_Accepted_NotSupportOnly()
    {
        Assert.True(new JwtService().TryParseAndValidate(Token("ROLE_ADMIN", "ROLE_USER"), "demo", out var user, out _));
        Assert.True(user!.IsAdmin);
        Assert.False(user.IsSupportOnly);
        Assert.Equal(HomeRoute.Admin, HomeRoute.For(user));
    }

    [Fact]
    public void Jwt_SupportOnly_Accepted_GoesToSupportHome()
    {
        Assert.True(new JwtService().TryParseAndValidate(Token("ROLE_SUPPORT", "ROLE_USER"), "demo", out var user, out _));
        Assert.False(user!.IsAdmin);
        Assert.True(user.IsSupportOnly);
        Assert.Equal(HomeRoute.Support, HomeRoute.For(user));
    }

    [Fact]
    public void Jwt_AdminAndSupport_IsAdmin()
    {
        Assert.True(new JwtService().TryParseAndValidate(Token("ROLE_SUPPORT", "ROLE_ADMIN"), "demo", out var user, out _));
        Assert.False(user!.IsSupportOnly);
        Assert.Equal(HomeRoute.Admin, HomeRoute.For(user));
    }

    [Theory]
    [InlineData("ROLE_USER")]
    [InlineData("ROLE_USER_INTERNET")]
    [InlineData("ROLE_CASHIER")]
    public void Jwt_OtherRoles_StillRefused(string role)
    {
        Assert.False(new JwtService().TryParseAndValidate(Token(role), "demo", out var user, out var error));
        Assert.Null(user);
        Assert.Equal("Ce compte n'a pas accès à l'application de gestion.", error);
    }

    [Fact]
    public void Jwt_SupportOfAnotherSite_Refused()
    {
        Assert.False(new JwtService().TryParseAndValidate(Token("ROLE_SUPPORT"), "autre", out _, out var error));
        Assert.Equal("Ce compte n'appartient pas à ce site.", error);
    }

    [Fact]
    public void HomeRoute_NoUser_IsAdminHome()
    {
        Assert.Equal(HomeRoute.Admin, HomeRoute.For(null));
    }

    [Fact]
    public void UserInfo_StoredSession_KeepsRolesAndIgnoresComputedFlags()
    {
        var json = JsonConvert.SerializeObject(new UserInfo { Id = 1, Roles = { "ROLE_SUPPORT" } });

        Assert.DoesNotContain("IsSupportOnly", json);
        Assert.True(JsonConvert.DeserializeObject<UserInfo>(json)!.IsSupportOnly);
    }
}
