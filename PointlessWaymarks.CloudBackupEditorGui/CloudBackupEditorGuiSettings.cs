using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CloudBackupEditorGui;

[NotifyPropertyChanged]
public partial class CloudBackupEditorGuiSettings
{
    public string DatabaseFile { get; set; } = string.Empty;
    public string LastDirectory { get; set; } = string.Empty;
    public string ProgramUpdateDirectory { get; set; } = @"M:\PointlessWaymarksPublications";
}