using System.Globalization;
using System.Windows.Data;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.LineList;

public class LineRecordedOnToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LineContent content) return string.Empty;

        var hoursAndMinutes = LineParts.LineDurationInHoursAndMinutes(content);

        if (string.IsNullOrWhiteSpace(hoursAndMinutes.presentationString)) return string.Empty;

        return hoursAndMinutes.presentationString;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}