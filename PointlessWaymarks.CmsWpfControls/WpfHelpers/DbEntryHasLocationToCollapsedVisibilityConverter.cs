using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.WpfHelpers;

public class DbEntryHasLocationToCollapsedVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is LineContent or GeoJsonContent or PointContent) return true;
        
        if (value is not PhotoContent content) return Visibility.Collapsed;

        if (content.Latitude == null || content.Longitude == null) return Visibility.Collapsed;

        return Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}