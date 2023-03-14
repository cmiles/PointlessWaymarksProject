using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PointlessWaymarks.WpfCommon.ValueConverters;

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => Visibility.Visible,
            bool b => b ? Visibility.Visible : Visibility.Collapsed,
            _ => Visibility.Visible
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility v)
            return v == Visibility.Visible;
        return false;
    }
}