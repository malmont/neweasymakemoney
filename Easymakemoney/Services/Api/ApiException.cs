using System.Net;
using Easymakemoney.Models.Api;

namespace Easymakemoney.Services.Api;

public class ApiException : Exception
{
    public ApiError ApiError { get; }
    public HttpStatusCode? StatusCode { get; }

    public ApiException(ApiError apiError, HttpStatusCode? statusCode = null, Exception? innerException = null)
        : base(apiError.Message, innerException)
    {
        ApiError = apiError;
        StatusCode = statusCode;
    }

    public static ApiException NoConnection(Exception? inner = null)
    {
        var error = new ApiError
        {
            Code = "NO_CONNECTION",
            Message = "Pas de connexion."
        };
        return new ApiException(error, null, inner);
    }
}

