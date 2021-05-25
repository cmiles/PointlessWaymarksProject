using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PointlessWaymarks.WpfCommon.ValueConverters
{
    public sealed class GreaterThanZeroIntIsVisibleElseHiddenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value as int?)
            {
                case null:
                    return Visibility.Visible;

                case < 1:
                    return Visibility.Collapsed;

                default:
                    return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}