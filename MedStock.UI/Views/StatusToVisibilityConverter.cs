using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MedStock.UI.Views
{
    public sealed class StatusToVisibilityConverter : IValueConverter
    {
        // value: Status string
        // parameter: target status string (e.g., "Draft")
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (value as string) ?? string.Empty;
            var target = (parameter as string) ?? string.Empty;

            if (string.Equals(status, target, StringComparison.OrdinalIgnoreCase))
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
