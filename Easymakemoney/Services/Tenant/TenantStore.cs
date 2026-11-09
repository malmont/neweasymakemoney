using Easymakemoney.Models.Tenant;

namespace Easymakemoney.Services.Tenant;

public class TenantStore : ITenantStore
{
    public const string TenantContextKey = "tenant_context";
    public const string FirstRunKey = "has_run_before";

    private TenantContext? _currentTenant;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public TenantContext? CurrentTenant => _currentTenant;

    public async Task<TenantContext?> GetTenantAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTenant != null)
        {
            return _currentTenant;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_currentTenant != null)
            {
                return _currentTenant;
            }

            var json = await SecureStorage.Default.GetAsync(TenantContextKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                _currentTenant = JsonConvert.DeserializeObject<TenantContext>(json);
            }

            return _currentTenant;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveTenantAsync(TenantContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _semaphore.WaitAsync();
        try
        {
            var json = JsonConvert.SerializeObject(context);
            await SecureStorage.Default.SetAsync(TenantContextKey, json);
            _currentTenant = context;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ClearAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            SecureStorage.Default.Remove(TenantContextKey);
            _currentTenant = null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void ClearMemory()
    {
        _currentTenant = null;
    }

    public Task EnsureFirstRunCleanAsync()
    {
        if (!Preferences.Default.ContainsKey(FirstRunKey))
        {
            try
            {
                SecureStorage.Default.RemoveAll();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TenantStore] Erreur purge premier lancement SecureStorage : {ex.Message}");
            }

            Preferences.Default.Set(FirstRunKey, true);
            ClearMemory();
        }

        return Task.CompletedTask;
    }
}

