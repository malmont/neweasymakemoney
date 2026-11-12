using Easymakemoney.Services.Offline;
using Easymakemoney.Services.Theme;

namespace Easymakemoney.Components;

public partial class AppHeader : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(AppHeader), string.Empty);

    public static readonly BindableProperty IsRootProperty =
        BindableProperty.Create(nameof(IsRoot), typeof(bool), typeof(AppHeader), false, propertyChanged: OnIsRootChanged);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool IsRoot
    {
        get => (bool)GetValue(IsRootProperty);
        set => SetValue(IsRootProperty, value);
    }

    public bool IsSubPage => !IsRoot;
    public string NavigationDescription => IsRoot ? "Ouvrir le menu" : "Retour";
    public ISiteBrandingService? Branding { get; }

    /// <summary>Lecture servie depuis la copie de l'appareil (OfflineCacheHandler) : bandeau « Hors connexion : données de 14 h 05 ».</summary>
    public bool IsOffline => OfflineState.ServedAt != null;
    public string OfflineText => OfflineState.Text(OfflineState.ServedAt);

    public AppHeader()
    {
        InitializeComponent();
        Branding = IPlatformApplication.Current?.Services.GetService<ISiteBrandingService>();
        HeaderRoot.BindingContext = this;
        Loaded += (_, _) => { OfflineState.Changed += OnOfflineChanged; OnOfflineChanged(); };
        Unloaded += (_, _) => OfflineState.Changed -= OnOfflineChanged;
    }

    private void OnOfflineChanged() => MainThread.BeginInvokeOnMainThread(() =>
    {
        OnPropertyChanged(nameof(IsOffline));
        OnPropertyChanged(nameof(OfflineText));
    });

    private static void OnIsRootChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var header = (AppHeader)bindable;
        header.OnPropertyChanged(nameof(IsSubPage));
        header.OnPropertyChanged(nameof(NavigationDescription));
    }

    private async void OnNavigationTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (IsRoot)
            {
                Shell.Current.FlyoutIsPresented = true;
            }
            else
            {
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppHeader] {ex.GetType().Name}");
        }
    }
}
