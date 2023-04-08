using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.SiteViewerGui;

public partial class SiteDirectoryListItem : ObservableObject
{
    [ObservableProperty] private DirectoryInfo _siteDirectory;
}