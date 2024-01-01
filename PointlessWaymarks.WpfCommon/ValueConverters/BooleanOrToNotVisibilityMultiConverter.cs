using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PointlessWaymarks.WpfCommon.ValueConverters;

public class BooleanOrToNotVisibilityMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values == null ? Visibility.Collapsed : values.Select(GetBool).Any(b => b) ? Visibility.Hidden : Visibility.Visible;
    }

    public object[]? ConvertBack(
        object value,
        Type[] targetTypes,
        object parameter,
        CultureInfo culture)
    {
        return null;
    }

    private static bool GetBool(object value) => value is true;
}