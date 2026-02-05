using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MedStock.UI.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // إذا كانت القيمة true يظهر العنصر
            if (value is bool booleanValue && booleanValue)
            {
                return Visibility.Visible;
            }
            // غير ذلك يختفي تماماً (Collapsed)
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visibility && visibility == Visibility.Visible;
        }
    }
}