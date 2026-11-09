using System.Globalization;

namespace Easymakemoney.Converters;

/// <summary>Affiche une date ISO du backend sous la forme jj/mm/aaaa.</summary>
public class IsoDateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string text && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            ? date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
            : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
