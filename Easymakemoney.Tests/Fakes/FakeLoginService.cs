using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Faux service de connexion enregistrant les appels à AuthenticateAsync et VerifyOtpAsync.</summary>
public class FakeLoginService : ILoginService
{
    public List<(string Username, string Password)> AuthenticateCalls { get; } = new();
    public List<(string Username, string Otp)> VerifyOtpCalls { get; } = new();

    public Func<string, string, (LoginResponse? Response, ApiError? Error)> OnAuthenticate { get; set; } =
        (_, _) => (null, new ApiError { Code = "UNEXPECTED", Message = "Réponse non configurée." });

    public Func<string, string, (LoginResponse? Response, ApiError? Error)> OnVerifyOtp { get; set; } =
        (_, _) => (null, new ApiError { Code = "UNEXPECTED", Message = "Réponse non configurée." });

    public Task<(LoginResponse? Response, ApiError? Error)> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        AuthenticateCalls.Add((username, password));
        return Task.FromResult(OnAuthenticate(username, password));
    }

    public Task<(LoginResponse? Response, ApiError? Error)> VerifyOtpAsync(
        string username,
        string otp,
        CancellationToken cancellationToken = default)
    {
        VerifyOtpCalls.Add((username, otp));
        return Task.FromResult(OnVerifyOtp(username, otp));
    }
}
