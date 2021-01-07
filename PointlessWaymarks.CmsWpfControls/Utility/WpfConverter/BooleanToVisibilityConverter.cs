using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PointlessWaymarks.CmsWpfControls.Utility.WpfConverter
{
    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case null:
                    return Visibility.Visible;

                case bool b:
                    return b ? Visibility.Visible : Visibility.Collapsed;

                default:
                    return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
                return v == Visibility.Visible;
            return false;
        }
    }
}