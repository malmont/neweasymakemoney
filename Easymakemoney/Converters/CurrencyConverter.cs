
using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.Converters
{
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            culture ??= CultureInfo.CurrentCulture;

            if (value == null)
            {
                return string.Empty;
            }

            double cents;
            if (value is long l)
                cents = l;
            else if (value is double d)
                cents = d;
            else if (value is int i)
                cents = i;
            else if (value is decimal m)
                cents = (double)m;
            else if (value is float f)
                cents = f;
            else if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedInv))
                cents = parsedInv;
            else if (double.TryParse(value.ToString(), NumberStyles.Any, culture, out var parsedCulture))
                cents = parsedCulture;
            else
                return value;

            return SiteCurrency.FormatCents(cents, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
