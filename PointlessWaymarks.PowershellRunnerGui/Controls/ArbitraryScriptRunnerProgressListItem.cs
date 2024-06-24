using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class ArbitraryScriptRunnerProgressListItem
{
    public string Message { get; set; } = string.Empty;
    public DateTime ReceivedOn { get; set; }
}