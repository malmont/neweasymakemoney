using Easymakemoney.Models.Tenant;

namespace Easymakemoney.Services.Site;

public interface ISiteService
{
    Task<SiteResult> ActivateWithKeyAsync(string rawKey, CancellationToken cancellationToken = default);
    Task<SiteResult> CheckSiteCodeAsync(string rawInput, CancellationToken cancellationToken = default);
}

