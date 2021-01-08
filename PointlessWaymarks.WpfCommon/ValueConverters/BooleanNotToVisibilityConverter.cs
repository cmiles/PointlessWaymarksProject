using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PointlessWaymarks.WpfCommon.ValueConverters
{
    public sealed class BooleanNotToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case null:
                    return Visibility.Visible;

                case bool b:
                    return b ? Visibility.Collapsed : Visibility.Visible;

                default:
                    return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
                return v == Visibility.Collapsed;
            return false;
        }
    }
}