using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.PointList;

public class MapMarkerColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var colorString = value?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(colorString)) return Brushes.Transparent;

        var colorLookup = PointContent.MapMarkerColorDictionary();

        if (!colorLookup.ContainsKey(colorString.ToLowerInvariant())) return Brushes.Transparent;

        var color = colorLookup[colorString.ToLowerInvariant()];

        return (SolidColorBrush)new BrushConverter().ConvertFrom(color);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}