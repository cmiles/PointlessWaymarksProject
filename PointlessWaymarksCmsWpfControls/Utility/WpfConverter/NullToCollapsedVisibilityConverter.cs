using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PointlessWaymarksCmsWpfControls.Utility.WpfConverter
{
    public sealed class NullToCollapsedVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                null => Visibility.Collapsed,
                _ => Visibility.Visible
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}