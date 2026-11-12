using Easymakemoney.Models.Tenant;
using Easymakemoney.Services.Tenant;

namespace Easymakemoney.Tests.Fakes;

public class FakeTenantStore : ITenantStore
{
    public TenantContext? CurrentTenant { get; set; }
    public int EnsureFirstRunCleanCalls { get; private set; }
    public int SaveCalls { get; private set; }
    public int ClearCalls { get; private set; }
    public int ClearMemoryCalls { get; private set; }

    public Task<TenantContext?> GetTenantAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CurrentTenant);
    }

    public Task SaveTenantAsync(TenantContext context)
    {
        SaveCalls++;
        CurrentTenant = context;
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        ClearCalls++;
        CurrentTenant = null;
        return Task.CompletedTask;
    }

    public void ClearMemory()
    {
        ClearMemoryCalls++;
        CurrentTenant = null;
    }

    public Task EnsureFirstRunCleanAsync()
    {
        EnsureFirstRunCleanCalls++;
        return Task.CompletedTask;
    }
}

