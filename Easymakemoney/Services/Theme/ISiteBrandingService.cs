namespace Easymakemoney.Services.Theme;

public interface ISiteBrandingService
{
    string SiteName { get; }
    string? LogoUrl { get; }
    bool HasLogo { get; }
    bool HasNoLogo { get; }
    string Initial { get; }

    /// <summary>Charge l'identité du site actif (une fois par site), ou revient aux valeurs par défaut sans site.</summary>
    Task EnsureLoadedAsync();
}
