using Easymakemoney.Services.Tenant;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Faux nettoyeur de données du site pour compter les appels dans les tests de ViewModel.</summary>
public class FakeSiteDataCleaner : ISiteDataCleaner
{
    public int ClearCalls { get; private set; }
    public Func<Task>? OnClearAllAsync { get; set; }

    public async Task ClearAllAsync()
    {
        ClearCalls++;
        if (OnClearAllAsync != null)
        {
            await OnClearAllAsync();
        }
    }
}

