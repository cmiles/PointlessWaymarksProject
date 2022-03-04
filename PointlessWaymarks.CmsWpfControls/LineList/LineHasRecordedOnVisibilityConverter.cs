using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.LineList;

public class LineHasRecordedOnVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not LineContent content) return Visibility.Collapsed;

        if (content.RecordingStartedOn == null && content.RecordingEndedOn == null) return Visibility.Collapsed;

        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}