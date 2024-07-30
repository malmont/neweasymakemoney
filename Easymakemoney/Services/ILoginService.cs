using System.Threading.Tasks;

namespace Easymakemoney.Services
{
    public interface ILoginService
    {
        Task<LoginResponse> Authenticate(LoginRequest loginRequest);
    }
}
