using System.Globalization;
using System.Windows.Data;
using Amazon.S3.Model.Internal.MarshallTransformations;

namespace PointlessWaymarks.WpfCommon.ValueConverters;

public sealed class BooleanNotConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => true,
            bool b => !b,
            _ => true
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}