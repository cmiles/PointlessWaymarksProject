using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CloudBackupGui;

[NotifyPropertyChanged]
public partial class CloudBackupGuiSettings
{
    public string DatabaseFile { get; set; } = string.Empty;
    public string? LastDirectory { get; set; } = string.Empty;
    public string ProgramUpdateDirectory { get; set; } = @"https://software.pointlesswaymarks.com/Software/PointlessWaymarksSoftwareList.json";
}