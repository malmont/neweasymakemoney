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

        // Le symbole dépend du pays courant (« $ » au Canada, « CA$ » ailleurs) : le cache est donc par devise ET par pays.
        // Avec la devise seule, le premier appel figeait le symbole pour toute la durée du processus, selon la culture de ce moment-là.
        var cacheKey = $"{code}|{SafeRegionName()}";
        if (_symbolCache.TryGetValue(cacheKey, out var cachedSymbol))
            return cachedSymbol;

        string discoveredSymbol = code;

        try
        {
            discoveredSymbol = FindSymbol(code) ?? code;
        }
        catch
        {
            // Repli sur le code ISO sans exception
            discoveredSymbol = code;
        }

        _symbolCache[cacheKey] = discoveredSymbol;
        return discoveredSymbol;
    }

    private static string SafeRegionName() => CurrentRegionOrNull()?.Name ?? string.Empty;

    /// <summary>
    /// Pays de la culture en cours. RegionInfo.CurrentRegion est mis en cache par .NET au premier appel et ne suit pas les changements
    /// de culture : on le recalcule depuis CultureInfo.CurrentCulture.
    /// </summary>
    private static RegionInfo? CurrentRegionOrNull()
    {
        try
        {
            var culture = CultureInfo.CurrentCulture;
            return string.IsNullOrEmpty(culture.Name) ? null : new RegionInfo(culture.Name);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// L'ordre des cultures varie d'un lancement à l'autre sur iOS : le choix ne doit pas en dépendre.
    /// Dans le pays de la devise, le symbole local (« $ » au Canada) ; ailleurs, le plus explicite (« CA$ »).
    /// </summary>
    private static string? FindSymbol(string code)
    {
        var current = CurrentRegionOrNull();
        if (current != null && string.Equals(current.ISOCurrencySymbol, code, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(current.CurrencySymbol))
        {
            return current.CurrencySymbol;
        }

        // Hors du pays de la devise : le symbole qui n'appartient qu'à elle, le plus court (« € » pour l'euro, « CA$ » pour le dollar
        // canadien, puisque « $ » est partagé par plusieurs devises). Sans symbole propre : le plus long, le plus explicite.
        if (!SymbolsByCurrency.Value.TryGetValue(code, out var symbols) || symbols.Count == 0)
            return null;

        var unique = symbols
            .Where(symbol => SymbolOwners.Value.TryGetValue(symbol, out var owners) && owners.Count == 1)
            .OrderBy(symbol => symbol.Length)
            .ThenBy(symbol => symbol, StringComparer.Ordinal)
            .FirstOrDefault();
        return unique ?? symbols
            .OrderByDescending(symbol => symbol.Length)
            .ThenBy(symbol => symbol, StringComparer.Ordinal)
            .First();
    }

    /// <summary>Symboles de chaque devise, relevés une fois dans toutes les cultures du système (indépendants de la culture courante).</summary>
    private static readonly Lazy<Dictionary<string, HashSet<string>>> SymbolsByCurrency = new(BuildSymbols);

    /// <summary>Pour chaque symbole, les devises qui l'utilisent.</summary>
    private static readonly Lazy<Dictionary<string, HashSet<string>>> SymbolOwners = new(() =>
    {
        var owners = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var (currency, symbols) in SymbolsByCurrency.Value)
            foreach (var symbol in symbols)
            {
                if (!owners.TryGetValue(symbol, out var set))
                    owners[symbol] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                set.Add(currency);
            }
        return owners;
    });

    private static Dictionary<string, HashSet<string>> BuildSymbols()
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            RegionInfo region;
            try
            {
                region = new RegionInfo(culture.Name);
            }
            catch
            {
                continue; // Culture non prise en charge par RegionInfo
            }

            var iso = region.ISOCurrencySymbol;
            var symbol = region.CurrencySymbol;
            if (string.IsNullOrWhiteSpace(iso) || string.IsNullOrWhiteSpace(symbol))
                continue;
            if (!map.TryGetValue(iso, out var set))
                map[iso] = set = new HashSet<string>(StringComparer.Ordinal);
            set.Add(symbol);
        }
        return map;
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

