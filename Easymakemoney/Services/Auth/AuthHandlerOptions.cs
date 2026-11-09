namespace Easymakemoney.Services.Auth;

public static class AuthHandlerOptions
{
    public static readonly HttpRequestOptionsKey<bool> SkipAuthKey = new("SkipAuth");

    public static void SetSkipAuth(this HttpRequestMessage request, bool skipAuth)
    {
        request.Options.Set(SkipAuthKey, skipAuth);
    }

    public static bool GetSkipAuth(this HttpRequestMessage request)
    {
        return request.Options.TryGetValue(SkipAuthKey, out var skip) && skip;
    }
}

