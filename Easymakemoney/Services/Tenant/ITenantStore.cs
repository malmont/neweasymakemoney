using Easymakemoney.Models.Tenant;

namespace Easymakemoney.Services.Tenant;

public interface ITenantStore
{
    TenantContext? CurrentTenant { get; }
    Task<TenantContext?> GetTenantAsync(CancellationToken cancellationToken = default);
    Task SaveTenantAsync(TenantContext context);
    Task ClearAsync();
    void ClearMemory();
    Task EnsureFirstRunCleanAsync();
}

