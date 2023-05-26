using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CloudBackupGui;

public partial class CloudBackupGuiSettings : ObservableObject
{
    [ObservableProperty] private string _programUpdateDirectory = @"M:\PointlessWaymarksPublications";
    [ObservableProperty] private string _lastDirectory = string.Empty;
}