using System.Globalization;
using System.Windows.Data;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.WpfCommon.ValueConverters;

public sealed class MinutesToHoursMinutesSecondsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value)
        {
            case null:
                return null;
            case double doubleMinutes:
                return TimeSpan.FromMinutes(doubleMinutes).ToString(@"hh\:mm\:ss");
            case decimal decimalMinutes:
                return TimeSpan.FromMinutes((double)decimalMinutes).ToString(@"hh\:mm\:ss");
            case int intMinutes:
                return TimeSpan.FromMinutes((double)intMinutes).ToString(@"hh\:mm\:ss");
        }

        if (double.TryParse(value.ToString(), out var stringParsedMinutes))
            return TimeSpan.FromMinutes(stringParsedMinutes).ToString(@"hh\:mm\:ss");
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

public sealed class SecondsToHoursMinutesSecondsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value)
        {
            case null:
                return null;
            case double doubleSeconds:
                return TimeSpan.FromSeconds(doubleSeconds).ToString(@"hh\:mm\:ss");
            case decimal decimalSeconds:
                return TimeSpan.FromSeconds((double)decimalSeconds).ToString(@"hh\:mm\:ss");
            case int intSeconds:
                return TimeSpan.FromSeconds((double)intSeconds).ToString(@"hh\:mm\:ss");
        }

        if (double.TryParse(value.ToString(), out var stringParsedSeconds))
            return TimeSpan.FromSeconds(stringParsedSeconds).ToString(@"hh\:mm\:ss");
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