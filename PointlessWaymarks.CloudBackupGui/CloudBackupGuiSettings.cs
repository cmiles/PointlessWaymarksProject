using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CloudBackupGui;

[NotifyPropertyChanged]
public partial class CloudBackupGuiSettings
{
    public string LastDirectory { get; set; } = string.Empty;
    public string ProgramUpdateDirectory { get; set; } = @"M:\PointlessWaymarksPublications";
}