using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;

namespace PointlessWaymarks.SiteViewerGui;

public partial class ProjectFileListItem : ObservableObject
{
    [ObservableProperty] private UserSettings _parsedSettings;
    [ObservableProperty] private FileInfo _settingsFile;
}