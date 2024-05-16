using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PointlessWaymarks.WpfCommon.ValueConverters;

public sealed class S3ProviderIsAmazonCollapsedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return Visibility.Visible;
        var stringValue = value.ToString();
        if(string.IsNullOrWhiteSpace(stringValue)) return Visibility.Visible;
        if(stringValue.Trim().Equals("amazon", StringComparison.OrdinalIgnoreCase)) return Visibility.Visible;
        return Visibility.Collapsed;
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}