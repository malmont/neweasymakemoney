
namespace Easymakemoney.Services
{
    public interface IJwtService
    {
          (string Username, string Role, int UserId) ParseToken(string token); 
    }
}
