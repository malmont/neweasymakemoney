using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Easymakemoney;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        ApplyStatusBar();
    }

    protected override void OnResume()
    {
        base.OnResume();
        ApplyStatusBar();
    }

    /// <summary>
    /// L'application est toujours en thème clair (App.UserAppTheme) : icônes et heure de la barre d'état en foncé,
    /// sinon elles sont blanches sur le fond clair des pages quand le téléphone est en mode sombre.
    /// </summary>
    private void ApplyStatusBar()
    {
        if (Window is not { } window)
            return;

        var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView);
        controller.AppearanceLightStatusBars = true;
        controller.AppearanceLightNavigationBars = true;
    }
}
