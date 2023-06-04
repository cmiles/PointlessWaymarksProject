using System.Globalization;
using System.Windows.Data;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.LineList;

public class LineRecordedOnSearchLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LineContent content) return string.Empty;

        if (content.RecordingStartedOn == null && content.RecordingEndedOn == null) return string.Empty;

        var recordedOn = LineContentActions.SearchRecordedDatesForPhotoContentDateRange(content);

        if(recordedOn.start.Date == recordedOn.end.AddDays(-1).Date) return $"Photos from {recordedOn.start:M/d/yyyy}";

        return $"Photos {recordedOn.start:M/d/yyyy} to {recordedOn.end:M/d/yyyy}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}