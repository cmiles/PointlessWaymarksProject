using System.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsGui;

[NotifyPropertyChanged]
public partial class SettingsFileListItem
{
    public UserSettings ParsedSettings { get; set; }
    public FileInfo SettingsFile { get; set; }
}