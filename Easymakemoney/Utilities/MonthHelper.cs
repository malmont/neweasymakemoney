using System.Globalization;

namespace Easymakemoney.Utilities;

public static class MonthHelper
{
    private static readonly string[] FrenchMonths = new[]
    {
        "janvier", "février", "mars", "avril", "mai", "juin",
        "juillet", "août", "septembre", "octobre", "novembre", "décembre"
    };

    /// <summary>
    /// Fonction pure et testable.
    /// Convertit un mois (entier 1..12 ou chaîne numérique "1".."12") en nom de mois français (janvier, février…).
    /// Une valeur non numérique ou hors bornes reste affichée telle quelle.
    /// </summary>
    public static string FormatMonthName(string? monthValue)
    {
        if (string.IsNullOrWhiteSpace(monthValue))
            return string.Empty;

        var trimmed = monthValue.Trim();

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var monthNumber))
        {
            if (monthNumber >= 1 && monthNumber <= 12)
            {
                return FrenchMonths[monthNumber - 1];
            }
        }

        // Valeur non numérique ou hors bornes 1..12 : reste affichée telle quelle
        return monthValue;
    }
}

