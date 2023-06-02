using System.IO;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.SiteViewerGui;

[NotifyPropertyChanged]
public partial class SiteDirectoryListItem
{
    public DirectoryInfo SiteDirectory { get; set; }
}