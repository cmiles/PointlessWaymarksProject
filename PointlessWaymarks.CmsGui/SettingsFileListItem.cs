using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;

namespace PointlessWaymarks.CmsGui;

public partial class SettingsFileListItem : ObservableObject
{
    [ObservableProperty] private UserSettings _parsedSettings;
    [ObservableProperty] private FileInfo _settingsFile;
}