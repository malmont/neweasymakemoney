using System.Globalization;

namespace Easymakemoney.Services.Incidents;

/// <summary>Ce que l'appareil peut dire de lui sans donnée personnelle : modèle (jamais son nom), système, langue, version.</summary>
public interface IIncidentEnvironment
{
    string? AppVersion { get; }
    string? Os { get; }
    string? OsVersion { get; }
    string? Device { get; }
    string? Locale { get; }
    bool IsOnline { get; }
    string? CurrentLocation { get; }
}

public class MauiIncidentEnvironment : IIncidentEnvironment
{
    public string? AppVersion => Safe(() => $"{AppInfo.Current.VersionString}+{AppInfo.Current.BuildString}");

    public string? Os => Safe(() =>
    {
        var platform = DeviceInfo.Current.Platform;
        if (platform == DevicePlatform.Android) return "android";
        if (platform == DevicePlatform.iOS) return "ios";
        if (platform == DevicePlatform.WinUI) return "windows";
        if (platform == DevicePlatform.MacCatalyst || platform == DevicePlatform.macOS) return "macos";
        return null;
    });

    public string? OsVersion => Safe(() => DeviceInfo.Current.VersionString);

    /// <summary>Le modèle seulement (SM-A546B, iPhone15,2) : DeviceInfo.Name contient souvent un prénom.</summary>
    public string? Device => Safe(() => DeviceInfo.Current.Model);

    public string? Locale => Safe(() => CultureInfo.CurrentUICulture.Name);

    public bool IsOnline => SafeBool(() => Connectivity.Current.NetworkAccess == NetworkAccess.Internet);

    public string? CurrentLocation => Safe(() => Shell.Current?.CurrentState?.Location?.OriginalString);

    private static string? Safe(Func<string?> read)
    {
        try { return read(); } catch (Exception) { return null; }
    }

    private static bool SafeBool(Func<bool> read)
    {
        try { return read(); } catch (Exception) { return false; }
    }
}
