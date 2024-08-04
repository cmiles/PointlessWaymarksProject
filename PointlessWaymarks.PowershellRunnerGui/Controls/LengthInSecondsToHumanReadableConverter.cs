using System.Globalization;
using System.Windows.Data;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

public class LengthInSecondsToHumanReadableConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int lengthInSeconds) return string.Empty;
        return TimeSpan.FromSeconds(lengthInSeconds).ToString(@"hh\:mm\:ss");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}