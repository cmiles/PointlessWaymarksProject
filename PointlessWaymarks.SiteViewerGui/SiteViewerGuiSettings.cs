using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.SiteViewerGui;

[NotifyPropertyChanged]
public partial class SiteViewerGuiSettings
{
    public string ProgramUpdateDirectory { get; set; } = @"https://software.pointlesswaymarks.com/Software/PointlessWaymarksSoftwareList.json";
}