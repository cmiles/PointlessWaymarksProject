using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.FilesWrittenLogList;

[ObservableObject]
public partial class FileWrittenLogListDateTimeFilterChoice
{
    [ObservableProperty] private string _displayText = string.Empty;
    [ObservableProperty] private DateTime? _filterDateTimeUtc;
}