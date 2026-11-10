using System.Globalization;

namespace Easymakemoney.Converters;

/// <summary>
/// Lit un nombre saisi avec une virgule ou un point. Sans ce convertisseur, MAUI lit la saisie en culture
/// invariante : « 12,50 » y devient 1250, la virgule étant prise pour un séparateur de milliers.
/// </summary>
public class DecimalInputConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double number ? number.ToString("0.##", culture) : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Parse(value as string);

    public static double? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var cleaned = text.Trim().Replace(" ", string.Empty).Replace(" ", string.Empty).Replace(" ", string.Empty).Replace(',', '.');

        // Plusieurs séparateurs : seul le dernier sépare les décimales.
        var last = cleaned.LastIndexOf('.');
        if (last >= 0)
        {
            cleaned = cleaned[..last].Replace(".", string.Empty) + cleaned[last..];
        }

        return double.TryParse(cleaned, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}
