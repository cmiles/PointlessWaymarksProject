using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.PowerShellRunnerGui;

[NotifyPropertyChanged]
public partial class PowerShellRunnerGuiSettings
{
    public string DatabaseFile { get; set; } = string.Empty;
    public string? LastDirectory { get; set; } = string.Empty;
    public string ProgramUpdateDirectory { get; set; } = @"https://software.pointlesswaymarks.com/Software/PointlessWaymarksSoftwareList.json";
}