using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.GeoToolsGui.Settings;

[NotifyPropertyChanged]
public class ConnectDownloadSettings
{
    public string ArchiveDirectory { get; set; } = string.Empty;
}