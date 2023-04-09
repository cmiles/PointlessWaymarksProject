using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.GeoToolsGui;

public partial class GeoToolsGuiSettings : ObservableObject
{
    [ObservableProperty] private string _programUpdateDirectory = @"M:\PointlessWaymarksPublications";
}