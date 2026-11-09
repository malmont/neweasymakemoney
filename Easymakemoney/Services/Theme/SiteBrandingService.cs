using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Easymakemoney.Services.Tenant;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Services.Theme;

/// <summary>
/// Identité et apparence du site actif : nom et logo (GET app-profile/admin), couleur, rayon et style des boutons
/// (GET mobile/app-theme?app=admin). Une valeur absente laisse le réglage par défaut de l'application.
/// </summary>
public partial class SiteBrandingService : ObservableObject, ISiteBrandingService
{
    private const string CacheKey = "site_branding";
    private const string DefaultPrimary = "#512BD4";
    private const double DefaultCardRadius = 16;
    private const double DefaultButtonRadius = 14;
    private const double PillRadius = 26;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITenantStore _tenantStore;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _loadedCode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Initial))]
    private string _siteName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLogo))]
    [NotifyPropertyChangedFor(nameof(HasNoLogo))]
    private string? _logoUrl;

    public bool HasLogo => !string.IsNullOrWhiteSpace(LogoUrl);
    public bool HasNoLogo => !HasLogo;
    public string Initial => string.IsNullOrWhiteSpace(SiteName) ? string.Empty : SiteName.Trim()[..1].ToUpperInvariant();

    public SiteBrandingService(IHttpClientFactory httpClientFactory, ITenantStore tenantStore)
    {
        _httpClientFactory = httpClientFactory;
        _tenantStore = tenantStore;
    }

    public async Task EnsureLoadedAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var tenant = await _tenantStore.GetTenantAsync();
            if (tenant == null)
            {
                if (_loadedCode != null)
                {
                    _loadedCode = null;
                    await ApplyAsync(string.Empty, null, null, null, null);
                }
                return;
            }

            if (string.Equals(_loadedCode, tenant.Code, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await ApplyCachedAsync(tenant.Code, tenant.Name);
            _loadedCode = tenant.Code;

            var client = _httpClientFactory.CreateClient("auth");
            var profile = await GetJsonAsync(client, "app-profile/admin?locale=fr");
            var theme = (await GetJsonAsync(client, "mobile/app-theme?app=admin"))?["theme"] as JObject;
            if (profile == null && theme == null)
            {
                return; // réseau indisponible : on garde le cache ou les valeurs par défaut
            }

            var name = Text(profile?["name"]) ?? tenant.Name;
            var logo = Text(profile?["logo"]);
            var primary = Text(theme?["primaryColor"]) ?? Text(profile?["primaryColor"]);
            var radius = theme?["radius"]?.Type == JTokenType.Integer ? theme["radius"]!.Value<double?>() : null;
            var buttonStyle = Text(theme?["buttonStyle"]);

            await ApplyAsync(name, logo, primary, radius, buttonStyle);
            Preferences.Default.Set(CacheKey, new JObject
            {
                ["code"] = tenant.Code, ["name"] = name, ["logo"] = logo, ["primary"] = primary,
                ["radius"] = radius, ["buttonStyle"] = buttonStyle
            }.ToString(Newtonsoft.Json.Formatting.None));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SiteBranding] {ex.GetType().Name}");
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ApplyCachedAsync(string code, string fallbackName)
    {
        var raw = Preferences.Default.Get(CacheKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(raw))
        {
            var cached = JObject.Parse(raw);
            if (string.Equals(Text(cached["code"]), code, StringComparison.OrdinalIgnoreCase))
            {
                await ApplyAsync(Text(cached["name"]) ?? fallbackName, Text(cached["logo"]), Text(cached["primary"]),
                    cached["radius"]?.Type == JTokenType.Float || cached["radius"]?.Type == JTokenType.Integer ? cached["radius"]!.Value<double?>() : null,
                    Text(cached["buttonStyle"]));
                return;
            }
        }

        await ApplyAsync(fallbackName, null, null, null, null);
    }

    private static async Task<JObject?> GetJsonAsync(HttpClient client, string url)
    {
        try
        {
            using var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return JObject.Parse(await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or Newtonsoft.Json.JsonException)
        {
            return null;
        }
    }

    private static string? Text(JToken? token) =>
        token == null || token.Type != JTokenType.String || string.IsNullOrWhiteSpace(token.Value<string>()) ? null : token.Value<string>();

    private Task ApplyAsync(string name, string? logo, string? primaryHex, double? radius, string? buttonStyle) =>
        MainThread.InvokeOnMainThreadAsync(() =>
        {
            SiteName = name;
            LogoUrl = logo;

            var primary = ParseColor(primaryHex) ?? Color.FromArgb(DefaultPrimary);
            var card = radius ?? DefaultCardRadius;
            var resources = Application.Current!.Resources;
            resources["ThemePrimary"] = primary;
            resources["ThemePrimaryTint"] = primary.WithAlpha(0.1f);
            resources["ThemeOnPrimary"] = Luminance(primary) > 0.45 ? Color.FromArgb("#0F172A") : Colors.White;
            resources["ThemeCardRadius"] = card;
            resources["ThemeButtonRadius"] = string.Equals(buttonStyle, "pill", StringComparison.OrdinalIgnoreCase) ? PillRadius : (radius ?? DefaultButtonRadius);
        });

    private static Color? ParseColor(string? hex) =>
        hex != null && hex.Length == 7 && hex[0] == '#' && int.TryParse(hex[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)
            ? Color.FromArgb(hex)
            : null;

    private static double Luminance(Color c) => 0.2126 * c.Red + 0.7152 * c.Green + 0.0722 * c.Blue;
}
