using Easymakemoney.Models.Api;

namespace Easymakemoney.Models.Tenant;

public class SiteResult
{
    public bool IsSuccess { get; private init; }
    public TenantContext? Tenant { get; private init; }
    public string? ErrorMessage { get; private init; }
    public ApiError? ApiError { get; private init; }

    public static SiteResult Success(TenantContext tenant) => new()
    {
        IsSuccess = true,
        Tenant = tenant
    };

    public static SiteResult Failure(string message, ApiError? apiError = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = message,
        ApiError = apiError
    };
}

