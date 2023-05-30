using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.GeoToolsGui.Settings;

[NotifyPropertyChanged]
public partial class ConnectDownloadSettings
{
    public string ArchiveDirectory { get; set; } = string.Empty;
}