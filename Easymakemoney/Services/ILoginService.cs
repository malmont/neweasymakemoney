using Easymakemoney.Models.Api;

namespace Easymakemoney.Services
{
	public interface ILoginService
	{
		Task<(LoginResponse? Response, ApiError? Error)> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
		Task<(LoginResponse? Response, ApiError? Error)> VerifyOtpAsync(string username, string otp, CancellationToken cancellationToken = default);
	}
}
