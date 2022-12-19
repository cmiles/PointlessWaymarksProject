#region

using CommunityToolkit.Mvvm.ComponentModel;

#endregion

namespace PointlessWaymarks.GeoToolsGui.Settings;

[ObservableObject]
public partial class ConnectBasedGeoTaggerSettings
{
    [ObservableProperty] private string _archiveDirectory = string.Empty;
    [ObservableProperty] private bool _createBackups = true;
    [ObservableProperty] private bool _createBackupsInDefaultStorage = true;
    [ObservableProperty] private string _exifToolFullName = string.Empty;
    [ObservableProperty] private string _filesToTagLastDirectoryFullName = string.Empty;
    [ObservableProperty] private bool _overwriteExistingGeoLocation;
    [ObservableProperty] private int _pointsMustBeWithinMinutes = 10;
}