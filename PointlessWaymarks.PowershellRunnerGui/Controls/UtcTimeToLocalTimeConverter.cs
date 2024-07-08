using System.Globalization;
using System.Windows.Data;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

public class UtcTimeToLocalTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime utcTime) return null;
        return utcTime.ToLocalTime();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}