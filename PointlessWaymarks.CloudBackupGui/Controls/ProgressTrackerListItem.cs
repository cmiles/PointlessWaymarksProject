using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
public partial class ProgressTrackerListItem
{
    public string Message { get; set; } = string.Empty;
    public DateTime ReceivedOn { get; set; }
}