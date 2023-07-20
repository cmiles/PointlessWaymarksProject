using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CloudBackupEditorGui.Controls;

[NotifyPropertyChanged]
public partial class ProgressTrackerListItem
{
    public string Message { get; set; } = string.Empty;
    public DateTime ReceivedOn { get; set; }
}