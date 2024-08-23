using System;
using System.Globalization;
using System.Linq;
using Microsoft.Maui.Controls;

namespace Easymakemoney.Converters
{
    public class FirstItemNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Collections.IEnumerable enumerable)
            {
                var firstItem = enumerable.Cast<object>().FirstOrDefault();
                if (firstItem != null)
                {
                    var property = firstItem.GetType().GetProperty("name");
                    if (property != null)
                    {
                        return property.GetValue(firstItem)?.ToString();
                    }
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
