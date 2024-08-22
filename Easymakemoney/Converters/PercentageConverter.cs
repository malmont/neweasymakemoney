
using System.Globalization;

namespace Easymakemoney.Converters
{
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            if (value is double doubleValue)
            {
                // Arrondir à 4 chiffres après la virgule et ajouter le symbole de pourcentage
                return $"{Math.Round(doubleValue, 4)}%";
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
