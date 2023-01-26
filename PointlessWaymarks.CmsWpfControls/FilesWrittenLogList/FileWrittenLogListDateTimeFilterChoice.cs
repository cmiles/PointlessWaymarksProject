using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.FilesWrittenLogList;

public partial class FileWrittenLogListDateTimeFilterChoice : ObservableObject
{
    [ObservableProperty] private string _displayText = string.Empty;
    [ObservableProperty] private DateTime? _filterDateTimeUtc;
}