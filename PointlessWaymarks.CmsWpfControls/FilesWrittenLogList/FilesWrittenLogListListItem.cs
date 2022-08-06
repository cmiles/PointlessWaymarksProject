#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.FilesWrittenLogList;

[ObservableObject]
public partial class FilesWrittenLogListListItem
{
    [ObservableProperty] private string _fileBase = string.Empty;
    [ObservableProperty] private bool _isInGenerationDirectory;
    [ObservableProperty] private string _transformedFile = string.Empty;
    [ObservableProperty] private string _writtenFile = string.Empty;
    [ObservableProperty] private DateTime _writtenOn;
}