using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PointlessWaymarks.WpfCommon.ValueConverters;

public class AnyNotVisibleToHiddenMultiConverter : IMultiValueConverter
{
    public object? Convert(object[]? values, Type? targetType, object? parameter, CultureInfo culture)
    {
        if (values == null) return Visibility.Hidden;
        if (values.Length == 0) return Visibility.Hidden;

        foreach (var valueLoop in values)
        {
            if (valueLoop is Visibility.Visible) continue;
            return Visibility.Hidden;
        }

        return Visibility.Visible;
    }

    public object[]? ConvertBack(object? value, Type[]? targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}