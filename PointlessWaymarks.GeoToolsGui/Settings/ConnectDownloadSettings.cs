using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.GeoToolsGui.Settings;

public partial class ConnectDownloadSettings : ObservableObject
{
    [ObservableProperty] private string _archiveDirectory = string.Empty;
}