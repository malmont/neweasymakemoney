using Easymakemoney.Models.Api;
using Easymakemoney.Models.Session;

namespace Easymakemoney.Models.Login;

public class LoginResult
{
    public bool IsSuccess { get; private init; }
    public bool OtpRequired { get; private init; }
    public string? Email { get; private init; }
    public UserInfo? User { get; private init; }
    public string? ErrorMessage { get; private init; }
    public ApiError? ApiError { get; private init; }

    public static LoginResult Success(UserInfo user) => new()
    {
        IsSuccess = true,
        User = user
    };

    public static LoginResult RequireOtp(string email) => new()
    {
        IsSuccess = false,
        OtpRequired = true,
        Email = email
    };

    public static LoginResult Failure(string errorMessage, ApiError? apiError = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        ApiError = apiError
    };
}

