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

    /// <summary>Lecture répétée (ex. tableau de santé relu toutes les 5 s) : un 401 ne rafraîchit pas le jeton, la session peut s'éteindre.</summary>
    public static readonly HttpRequestOptionsKey<bool> SkipRefreshKey = new("SkipRefresh");

    public static void SetSkipRefresh(this HttpRequestMessage request, bool skipRefresh)
    {
        request.Options.Set(SkipRefreshKey, skipRefresh);
    }

    public static bool GetSkipRefresh(this HttpRequestMessage request)
    {
        return request.Options.TryGetValue(SkipRefreshKey, out var skip) && skip;
    }
}
