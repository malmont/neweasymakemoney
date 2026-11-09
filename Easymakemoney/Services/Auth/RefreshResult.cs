using System.Net;

namespace Easymakemoney.Services.Auth;

public enum RefreshStatus
{
    Success,
    Rejected,
    NetworkError
}

public class RefreshResult
{
    public RefreshStatus Status { get; private init; }
    public string? NewToken { get; private init; }
    public HttpStatusCode? StatusCode { get; private init; }

    public bool IsSuccess => Status == RefreshStatus.Success;

    public static RefreshResult Success(string newToken) => new()
    {
        Status = RefreshStatus.Success,
        NewToken = newToken
    };

    public static RefreshResult Rejected(HttpStatusCode? statusCode = null) => new()
    {
        Status = RefreshStatus.Rejected,
        StatusCode = statusCode
    };

    public static RefreshResult NetworkError() => new()
    {
        Status = RefreshStatus.NetworkError
    };
}

