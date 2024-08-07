
namespace Easymakemoney.Services
{
    public class JwtService : IJwtService
    {
        public (string Username, string Role, int UserId) ParseToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token) as JwtSecurityToken;

            var username = jsonToken.Claims.First(claim => claim.Type == "username").Value;
            var role = jsonToken.Claims.First(claim => claim.Type == "roles").Value;
            var userId = int.Parse(jsonToken.Claims.First(claim => claim.Type == "id").Value); // Extraire l'ID de l'utilisateur

            return (username, role, userId);
        }
    }
}