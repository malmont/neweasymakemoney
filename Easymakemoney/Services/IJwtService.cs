
namespace Easymakemoney.Services
{
    public interface IJwtService
    {
        (string Username, string Role) ParseToken(string token);
    }
}
