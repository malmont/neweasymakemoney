using System.Collections.Concurrent;
using System.Globalization;

namespace Easymakemoney.Utilities;

public static class SiteCurrency
{
    private static string? _currentCurrency;
    private static readonly object _lock = new();
    private static readonly ConcurrentDictionary<string, string> _symbolCache = new(StringComparer.OrdinalIgnoreCase);

    public static string? CurrentCurrency
    {
        get
        {
            lock (_lock) return _currentCurrency;
        }
    }

    public static void SetCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) return;
        lock (_lock)
        {
            _currentCurrency = currency.Trim().ToUpperInvariant();
        }
    }

    public static void Reset()
    {
        lock (_lock)
        {
            _currentCurrency = null;
        }
    }

    /// <summary>
    /// Découvre et met en cache le symbole monétaire associé au code ISO (R1).
    /// Aucun symbole écrit en dur : exploration des cultures du système via RegionInfo.
    /// Si la recherche échoue ou ne trouve rien, repli sur le code ISO, sans exception.
    /// </summary>
    public static string GetCurrencySymbol(string isoCurrencyCode)
    {
        if (string.IsNullOrWhiteSpace(isoCurrencyCode))
            return string.Empty;

        var code = isoCurrencyCode.Trim().ToUpperInvariant();

        if (_symbolCache.TryGetValue(code, out var cachedSymbol))
            return cachedSymbol;

        string discoveredSymbol = code;

        try
        {
            foreach (var ci in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                try
                {
                    var ri = new RegionInfo(ci.Name);
                    if (string.Equals(ri.ISOCurrencySymbol, code, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(ri.CurrencySymbol))
                        {
                            discoveredSymbol = ri.CurrencySymbol;
                            break;
                        }
                    }
                }
                catch
                {
                    // Ignore les cultures spécifiques non supportées par RegionInfo
                }
            }
        }
        catch
        {
            // Repli sur le code ISO sans exception
            discoveredSymbol = code;
        }

        _symbolCache[code] = discoveredSymbol;
        return discoveredSymbol;
    }

    /// <summary>
    /// Fonction pure qui formate un montant en centimes.
    /// La devise vient de currencyCode (ou de SiteCurrency si omis).
    /// La culture sert pour la langue et les séparateurs.
    /// Sans code connu : affiche le nombre sans symbole.
    /// Aucun symbole écrit en dur.
    /// </summary>
    public static string FormatCents(double cents, CultureInfo? culture = null, string? currencyCode = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        currencyCode ??= CurrentCurrency;

        double amount = cents / 100.0;

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            // Sans code connu : affiche le nombre sans symbole avec les séparateurs de la culture
            return amount.ToString("N2", culture);
        }

        var symbol = GetCurrencySymbol(currencyCode);
        var nfi = (NumberFormatInfo)culture.NumberFormat.Clone();
        nfi.CurrencySymbol = symbol;

        return amount.ToString("C", nfi);
    }
}

