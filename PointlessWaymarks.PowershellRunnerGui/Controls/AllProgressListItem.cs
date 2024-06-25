using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class AllProgressListItem
{
    public string Message { get; set; } = string.Empty;
    public DateTime ReceivedOn { get; set; }
    public string? Sender { get; set; }
}