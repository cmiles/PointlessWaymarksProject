using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.SiteViewerGui;

public partial class SiteViewerGuiSettings : ObservableObject
{
    [ObservableProperty] private string _programUpdateDirectory = @"M:\PointlessWaymarksPublications";
}