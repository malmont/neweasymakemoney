
namespace Easymakemoney.Services
{
    public class JwtService : IJwtService
    {
        public (string Username, string Role) ParseToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token) as JwtSecurityToken;

            var username = jsonToken.Claims.First(claim => claim.Type == "username").Value;
            var role = jsonToken.Claims.First(claim => claim.Type == "roles").Value;

            return (username, role);
        }
    }
}
