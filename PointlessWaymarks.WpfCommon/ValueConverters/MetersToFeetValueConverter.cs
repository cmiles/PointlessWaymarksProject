using System.Globalization;
using System.Windows.Data;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.WpfCommon.ValueConverters;

public sealed class MetersToFeetValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value)
        {
            case null:
                return null;
            case double doubleElevation:
                return doubleElevation.MetersToFeet();
        }

        if (double.TryParse(value.ToString(), out var stringParsedElevation))
            return stringParsedElevation.MetersToFeet();
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value)
        {
            case null:
                return null;
            case double doubleElevation:
                return doubleElevation.FeetToMeters();
        }

        if (double.TryParse(value.ToString(), out var stringParsedElevation))
            return stringParsedElevation.FeetToMeters();
        return null;
    }
}