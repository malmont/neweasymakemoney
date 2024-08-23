using System;
using System.Globalization;

namespace Easymakemoney.Converters
{
    public class FirstLetterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrEmpty(str))
            {
                return str.Substring(0, 1).ToUpper(); // Récupère la première lettre en majuscule
            }

            return string.Empty; // Retourne une chaîne vide si la valeur est null ou vide
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
