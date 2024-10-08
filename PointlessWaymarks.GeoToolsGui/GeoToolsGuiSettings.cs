using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.GeoToolsGui;

[NotifyPropertyChanged]
public partial class GeoToolsGuiSettings
{
    public string ProgramUpdateDirectory { get; set; } = @"https://software.pointlesswaymarks.com/Software/PointlessWaymarksSoftwareList.json";
}