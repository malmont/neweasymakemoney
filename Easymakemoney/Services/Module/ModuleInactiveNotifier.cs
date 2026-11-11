namespace Easymakemoney.Services.Module;

public interface IModuleInactiveNotifier
{
    /// <summary>Dernier refus reçu (lu par l'écran « non activée »).</summary>
    ModuleInactiveInfo? Last { get; }

    Task NotifyAsync(ModuleInactiveInfo info);

    /// <summary>À appeler quand l'écran se ferme ou que l'essai est ouvert : le prochain refus rouvrira l'écran.</summary>
    void Reset();
}

/// <summary>
/// Ouvre une seule fois l'écran « Application de gestion non activée », même si plusieurs appels sont refusés
/// en même temps (accueil : plusieurs vignettes chargées ensemble).
/// </summary>
public class ModuleInactiveNotifier : IModuleInactiveNotifier
{
    public const string Route = "ModuleInactivePage";

    private int _shown;

    public ModuleInactiveInfo? Last { get; private set; }

    public async Task NotifyAsync(ModuleInactiveInfo info)
    {
        Last = info;
        if (Interlocked.CompareExchange(ref _shown, 1, 0) != 0)
            return;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    if (Shell.Current != null)
                        await Shell.Current.GoToAsync(Route);
                    else
                        Reset();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ModuleInactiveNotifier] Navigation impossible : {ex.Message}");
                    Reset();
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ModuleInactiveNotifier] MainThread : {ex.Message}");
            Reset();
        }
    }

    public void Reset() => Interlocked.Exchange(ref _shown, 0);
}
