namespace Easymakemoney.Services.Tenant;

public static class TenantHandlerOptions
{
    public static readonly HttpRequestOptionsKey<bool> SkipTenantHost = new("SkipTenantHost");
    public static readonly HttpRequestOptionsKey<string> CustomTenantHost = new("CustomTenantHost");

    public static HttpRequestMessage SetSkipTenantHost(this HttpRequestMessage request, bool skip = true)
    {
        request.Options.Set(SkipTenantHost, skip);
        return request;
    }

    public static HttpRequestMessage SetCustomTenantHost(this HttpRequestMessage request, string host)
    {
        request.Options.Set(CustomTenantHost, host);
        return request;
    }
}

