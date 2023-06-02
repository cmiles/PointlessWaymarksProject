using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.SiteViewerGui;

[NotifyPropertyChanged]
public partial class SiteViewerGuiSettings
{
    public string ProgramUpdateDirectory { get; set; } = @"M:\PointlessWaymarksPublications";
}