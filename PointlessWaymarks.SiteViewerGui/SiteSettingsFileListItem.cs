using System.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.SiteViewerGui;

[NotifyPropertyChanged]
public partial class SiteSettingsFileListItem
{
    public UserSettings ParsedSettings { get; set; }
    public FileInfo SettingsFile { get; set; }
}