using System.Diagnostics;

namespace Easymakemoney.Services.Auth;

public class SessionExpiryNotifier : ISessionExpiryNotifier
{
    private int _notified;

    public async Task NotifySessionExpiredAsync()
    {
        // 0 -> non notifié, 1 -> notifié
        if (Interlocked.CompareExchange(ref _notified, 1, 0) == 0)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        if (Shell.Current != null)
                        {
                            await Easymakemoney.Utilities.AppDialogs.InfoAsync("Session expirée", "Votre session a expiré, reconnectez-vous.");
                            await Shell.Current.GoToAsync("//LoginPage");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SessionExpiryNotifier] Erreur navigation expiration : {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SessionExpiryNotifier] Erreur MainThread : {ex.Message}");
            }
        }
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _notified, 0);
    }
}

