using Easymakemoney.Models.Tenant;
using Easymakemoney.Services.Tenant;

namespace Easymakemoney.Tests.Fakes;

public class FakeTenantStore : ITenantStore
{
    public TenantContext? CurrentTenant { get; set; }

    public Task<TenantContext?> GetTenantAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CurrentTenant);
    }

    public Task SaveTenantAsync(TenantContext context)
    {
        CurrentTenant = context;
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        CurrentTenant = null;
        return Task.CompletedTask;
    }

    public void ClearMemory()
    {
        CurrentTenant = null;
    }

    public Task EnsureFirstRunCleanAsync()
    {
        return Task.CompletedTask;
    }
}

