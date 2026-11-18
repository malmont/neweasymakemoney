#if ANDROID
using Android.Graphics;
#elif IOS
using UIKit;
#endif

namespace Easymakemoney
{
    public partial class App : Application
    {
        private readonly AppShell _shell;

        public App(IServiceProvider services)
        {
            InitializeComponent();

            // Mapping personnalisé pour BorderlessEntry
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry), (handler, view) =>
            {
                if (view is BorderlessEntry)
                {
#if ANDROID
                    handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
#elif IOS
                    handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
                }
            });

            // Mapping personnalisé pour BorderlessDatePicker
            Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping(nameof(BorderlessDatePicker), (handler, view) =>
            {
                if (view is BorderlessDatePicker)
                {
#if ANDROID
                    handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
#elif IOS
                    handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
                }
            });

            // Règle M10 : forcer le thème clair pour éviter le texte blanc sur fond blanc en mode sombre
            UserAppTheme = AppTheme.Light;

            // Pas de cadre natif : le champ est dessiné par son Border dans les formulaires
            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("UiBorderlessPicker", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
#elif IOS
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
            });

            // Le Shell est créé ici, après InitializeComponent : son XAML utilise les ressources de l'application.
            _shell = services.GetRequiredService<AppShell>();

            // Remontée des incidents (INC-2) : erreurs non attrapées, plantage noté pour le lancement suivant, file au retour du réseau
            if (services.GetService<Easymakemoney.Services.Incidents.IIncidentReporter>() is { } reporter)
            {
                AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                {
                    if (e.ExceptionObject is not Exception ex)
                        return;
                    if (e.IsTerminating)
                        reporter.WriteCrashMarker(ex);
                    else
                        reporter.ReportException(ex);
                };
                TaskScheduler.UnobservedTaskException += (_, e) => reporter.ReportException(e.Exception);
                try
                {
                    Connectivity.Current.ConnectivityChanged += (_, _) => _ = reporter.FlushAsync();
                }
                catch (Exception)
                {
                }
                _ = reporter.StartAsync();
            }
        }

        protected override Window CreateWindow(IActivationState? activationState) => new(_shell);
    }
}
