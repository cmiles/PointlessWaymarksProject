using System.Globalization;
using System.Windows.Data;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

public class DateTimeMaxValueToNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dateTime) return null;
        if (dateTime == DateTime.MaxValue) return null;
        return dateTime;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}