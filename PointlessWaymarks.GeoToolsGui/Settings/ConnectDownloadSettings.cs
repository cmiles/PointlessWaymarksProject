#region

using CommunityToolkit.Mvvm.ComponentModel;

#endregion

namespace PointlessWaymarks.GeoToolsGui.Settings;

[ObservableObject]
public partial class ConnectDownloadSettings
{
    [ObservableProperty] private string _archiveDirectory = string.Empty;
}