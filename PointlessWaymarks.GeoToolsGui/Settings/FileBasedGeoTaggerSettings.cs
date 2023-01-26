using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.GeoToolsGui.Settings;

public partial class FileBasedGeoTaggerSettings : ObservableObject
{
    [ObservableProperty] private bool _createBackups = true;
    [ObservableProperty] private bool _createBackupsInDefaultStorage = true;
    [ObservableProperty] private string _exifToolFullName = string.Empty;
    [ObservableProperty] private string _filesToTagLastDirectoryFullName = string.Empty;
    [ObservableProperty] private string _gpxLastDirectoryFullName = string.Empty;
    [ObservableProperty] private bool _overwriteExistingGeoLocation;
    [ObservableProperty] private int _pointsMustBeWithinMinutes = 10;
    [ObservableProperty] private bool _replaceExistingFiles;
}