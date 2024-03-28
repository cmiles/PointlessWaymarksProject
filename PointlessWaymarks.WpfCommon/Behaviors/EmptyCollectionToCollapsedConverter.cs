using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.WpfCommon.Behaviors;

public class EmptyCollectionToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var dynamicValue = value as dynamic;

        if (dynamicValue is not null && DynamicTypeTools.PropertyExists(dynamicValue, "Count"))
        {
            return dynamicValue.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}